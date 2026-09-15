using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Auth;
using SprintFlow.Web.Server.DTOs.Invitations;
using SprintFlow.Web.Server.Models;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

public class InvitationService
{
    private const int TokenBytes = 32;

    private const int MemberRoleId = 2;

    private const int DuplicateKeyError = 2601;
    private const int UniqueConstraintError = 2627;

    private readonly InvitationRepository invitationRepository;
    private readonly UserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly InvitationOptions options;

    public InvitationService(
        InvitationRepository invitationRepository,
        UserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOptions<InvitationOptions> options)
    {
        this.invitationRepository = invitationRepository;
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.options = options.Value;
    }

    public async Task<InvitationResponseDto> CreateAsync(
        int invitedByUserId,
        CancellationToken cancellationToken)
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

        var expiresAt = DateTime.UtcNow.AddHours(options.ExpiryHours);

        var invitation = new Invitation
        {
            TokenHash = HashToken(token),
            InvitedByUserId = invitedByUserId,
            ExpiresAt = expiresAt
        };

        invitationRepository.Add(invitation);

        await invitationRepository.SaveChangesAsync(cancellationToken);

        return new InvitationResponseDto(token, expiresAt);
    }

    public async Task<InvitationTokenStatus> GetStatusAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(
            HashToken(token), cancellationToken);

        return Evaluate(invitation, DateTime.UtcNow);
    }

    public async Task<(RegisterOutcome Outcome, InvitationTokenStatus TokenStatus, RegisterResponseDto? User)> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        if (Encoding.UTF8.GetByteCount(request.Password) > IPasswordHasher.MaxPasswordBytes)
        {
            return (RegisterOutcome.PasswordTooLong, InvitationTokenStatus.Valid, null);
        }

        var tokenHash = HashToken(request.Token);

        return await invitationRepository.RunRedemptionAsync(
            tokenHash,
            async token =>
            {
                var invitation = await invitationRepository.GetForUpdateAsync(tokenHash, token);

                var status = Evaluate(invitation, DateTime.UtcNow);

                if (status != InvitationTokenStatus.Valid)
                {
                    return (RegisterOutcome.InvitationNotUsable, status, (RegisterResponseDto?)null);
                }

                var email = AuthService.CanonicalizeEmail(request.Email);

                if (await userRepository.EmailExistsAsync(email, token))
                {
                    return (RegisterOutcome.EmailAlreadyRegistered, status, null);
                }

                var user = new User
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,

                    PasswordHash = passwordHasher.Hash(request.Password),

                    RoleId = MemberRoleId
                };

                userRepository.Add(user);

                try
                {
                    await userRepository.SaveChangesAsync(token);
                }
                catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
                {
                    return (RegisterOutcome.EmailAlreadyRegistered, status, null);
                }

                invitation!.UsedAt = DateTime.UtcNow;
                invitation.RegisteredUserId = user.UserId;

                await invitationRepository.SaveChangesAsync(token);

                return (
                    RegisterOutcome.Created,
                    status,
                    new RegisterResponseDto(user.FullName, user.Email));
            },
            cancellationToken);
    }

    private static byte[] HashToken(string token)
        => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    private static InvitationTokenStatus Evaluate(Invitation? invitation, DateTime utcNow)
    {
        if (invitation is null)
        {
            return InvitationTokenStatus.NotFound;
        }

        if (invitation.UsedAt is not null)
        {
            return InvitationTokenStatus.AlreadyUsed;
        }

        if (invitation.ExpiresAt <= utcNow)
        {
            return InvitationTokenStatus.Expired;
        }

        return invitation.InvitedByUser.IsActive
            ? InvitationTokenStatus.Valid
            : InvitationTokenStatus.InviterInactive;
    }

    private static bool IsUniquenessViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sql
           && sql.Number is DuplicateKeyError or UniqueConstraintError;
}
