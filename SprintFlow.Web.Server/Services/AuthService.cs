using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Auth;
using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Who the users are and how they sign in.
/// </summary>
public class AuthService
{
    private readonly UserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokenService;
    private readonly AuthOptions authOptions;

    public AuthService(
        UserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<AuthOptions> authOptions)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
        this.authOptions = authOptions.Value;
    }

    /// <summary>
    /// Seeding, registration and login must all use this, or stored addresses stop matching input.
    /// ToLowerInvariant, never ToLower: ToLower follows the machine culture and turns 'I' into 'ı'
    /// on a Turkish machine.
    /// </summary>
    public static string CanonicalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var email = CanonicalizeEmail(request.Email);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            // Verified against a dummy hash so response time cannot reveal whether the address
            // exists.
            passwordHasher.VerifyAgainstDummy(request.Password);
            throw new AuthenticationFailedException();
        }

        // Password checked before IsActive, so a disabled account is not distinguishable by timing.
        var passwordMatches = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordMatches || !user.IsActive)
        {
            throw new AuthenticationFailedException(); // One error for all three causes.
        }

        // Admin access is tied to UserId, never to the e-mail or the role name.
        var isAdmin = user.UserId == authOptions.AdminUserId;

        return new LoginResponseDto(user.UserId, tokenService.CreateToken(user), user.FullName, isAdmin);
    }

    /// <summary>The company directory. No password, hash or token is exposed.</summary>
    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users.Select(user => user.ToListItemDto()).ToList();
    }
}
