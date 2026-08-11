using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Common.Exceptions;
using SprintFlow.Web.Server.DTOs.Auth;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokenService;
    private readonly AuthOptions authOptions;

    public AuthService(
        IUserRepository userRepository,
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
        var email = CanonicalizeEmail(request.Email); // Normalize email for consistent lookup.

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            // Verify against a dummy hash to reduce timing differences; response time must not
            // reveal whether the email exists.
            passwordHasher.VerifyAgainstDummy(request.Password);
            throw new AuthenticationFailedException();
        }

        // Password is checked before IsActive so a disabled account is not distinguishable by timing.
        var passwordMatches = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordMatches || !user.IsActive)
        {
            throw new AuthenticationFailedException(); // One error for all three causes.
        }

        var isAdmin = user.UserId == authOptions.AdminUserId; // Admin access is tied to UserId, not email or role.

        return new LoginResponseDto(tokenService.CreateToken(user), user.FullName, isAdmin);
    }
}
