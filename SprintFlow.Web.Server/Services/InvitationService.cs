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

/// <summary>
/// Invitations, and the registration they gate.
/// </summary>
/// <remarks>
/// Accounts are not created by the public and are not created by hand: someone already signed in
/// produces a single-use link, and whoever holds that link may create exactly one account. Both ends
/// of that exchange are recorded, so it is always answerable who invited a given user.
/// <para>
/// A new account is always a Member. There is no property on the request for a role, so this is not
/// a check that could be forgotten — there is nothing for a caller to send.
/// </para>
/// </remarks>
public class InvitationService
{
    /// <summary>
    /// Bytes of randomness behind each token — 256 bits, so tokens cannot be guessed or enumerated
    /// and no rate limit is load-bearing for their secrecy.
    /// </summary>
    private const int TokenBytes = 32;

    /// <summary>
    /// The role every registered account receives, matching the row <c>RoleConfiguration</c> seeds
    /// with the schema. Registration never grants Admin, and admin access is not a role anyway —
    /// it is <c>UserId == Auth:AdminUserId</c>, which nothing here can change.
    /// </summary>
    private const int MemberRoleId = 2;

    /// <summary>SQL Server's two "you broke a uniqueness rule" errors: unique index, unique constraint.</summary>
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
        // Cryptographically secure by construction, and carrying nothing: not the inviter, not an
        // address, not a timestamp. Everything the token means is looked up by its digest.
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

        // Instants, not calendar days, so this reads the clock directly rather than through
        // IApplicationClock — that answers which day it is where the company works, which is a
        // different question and would drag a timezone into an expiry that has no business with one.
        var expiresAt = DateTime.UtcNow.AddHours(options.ExpiryHours);

        var invitation = new Invitation
        {
            // Only the digest is kept. Someone reading this table learns that an invitation exists
            // and who created it, but cannot redeem it.
            TokenHash = HashToken(token),
            InvitedByUserId = invitedByUserId,
            ExpiresAt = expiresAt

            // CreatedDate is intentionally not set; the database default supplies it.
        };

        invitationRepository.Add(invitation);

        await invitationRepository.SaveChangesAsync(cancellationToken);

        // The one and only time the token leaves this process. It is not logged here or anywhere.
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
        // Checked before the invitation is touched: it reads nothing but the request, so spending a
        // database round trip and an exclusive hold to decide it would be wasted work — and a
        // rejected password must leave the invitation usable for the next attempt.
        if (Encoding.UTF8.GetByteCount(request.Password) > IPasswordHasher.MaxPasswordBytes)
        {
            return (RegisterOutcome.PasswordTooLong, InvitationTokenStatus.Valid, null);
        }

        var tokenHash = HashToken(request.Token);

        // Everything below runs with an exclusive hold on this invitation's row. Two callers
        // presenting the same token are serialised by it: the second one's read happens only after
        // the first has committed, so it sees the invitation already spent instead of creating a
        // second account from it.
        return await invitationRepository.RunRedemptionAsync(
            tokenHash,
            async token =>
            {
                // Read inside the hold. Asked before it was taken, the answer could already be
                // stale by the time it was acted on — which is the whole failure this guards.
                var invitation = await invitationRepository.GetForUpdateAsync(tokenHash, token);

                var status = Evaluate(invitation, DateTime.UtcNow);

                if (status != InvitationTokenStatus.Valid)
                {
                    return (RegisterOutcome.InvitationNotUsable, status, (RegisterResponseDto?)null);
                }

                var email = AuthService.CanonicalizeEmail(request.Email);

                // The fast path, for a clear answer. Not the guarantee — two registrations for one
                // address can both pass this, and the unique index below is what actually stops the
                // second.
                if (await userRepository.EmailExistsAsync(email, token))
                {
                    return (RegisterOutcome.EmailAlreadyRegistered, status, null);
                }

                var user = new User
                {
                    // Stored without surrounding whitespace, as every other name in this project is.
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,

                    // The plaintext goes no further than this call.
                    PasswordHash = passwordHasher.Hash(request.Password),

                    // Never from the request: there is no property for it to arrive in.
                    RoleId = MemberRoleId

                    // IsActive and CreatedDate are intentionally not set; the database defaults
                    // supply true and the current instant.
                };

                userRepository.Add(user);

                try
                {
                    await userRepository.SaveChangesAsync(token);
                }
                catch (DbUpdateException exception) when (IsUniquenessViolation(exception))
                {
                    // Another registration took this address between the check above and this
                    // insert. The index did its job; the caller gets the same answer they would
                    // have got a moment earlier, and the transaction rolls back so neither the row
                    // nor the invitation's consumption survives.
                    return (RegisterOutcome.EmailAlreadyRegistered, status, null);
                }

                // Spent, in the same transaction that created the account. The two facts are
                // committed together or not at all.
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

    /// <summary>
    /// SHA-256 of the token as it was written, so creating and redeeming agree without the token
    /// ever having to be decoded — a malformed one simply digests to something no row holds.
    /// </summary>
    /// <remarks>
    /// A plain digest rather than BCrypt, and deliberately: the token is 256 bits of uniform
    /// randomness, so there is no low-entropy secret to slow an attacker down over, and no salt to
    /// need. A salted hash could not be indexed either — every row would have to be read and tested
    /// to find one invitation. Passwords are a different problem and keep BCrypt.
    /// </remarks>
    private static byte[] HashToken(string token)
        => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    /// <summary>
    /// Whether an invitation may still be redeemed, and if not, why. The causes are separated
    /// because a 256-bit token cannot be guessed, so naming the reason tells an attacker nothing
    /// and tells the person holding a stale link exactly what to do about it.
    /// </summary>
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

        // An account that can no longer sign in should not still be creating accounts through a
        // link it left behind.
        return invitation.InvitedByUser.IsActive
            ? InvitationTokenStatus.Valid
            : InvitationTokenStatus.InviterInactive;
    }

    /// <summary>
    /// Whether the write failed because it collided with a uniqueness rule — here, only ever the
    /// unique index on <c>Users.Email</c>, since that insert touches nothing else unique.
    /// </summary>
    /// <remarks>
    /// Without this the race would leave <c>ExceptionHandlingMiddleware</c> to turn a perfectly
    /// ordinary "that address is taken" into a 500.
    /// </remarks>
    private static bool IsUniquenessViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sql
           && sql.Number is DuplicateKeyError or UniqueConstraintError;
}
