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
    /// The one place that turns a submitted address into the form stored in the database. Seeding
    /// and any future registration must use the same rule, or a stored address will stop matching
    /// what a user types.
    /// </summary>
    /// <remarks>
    /// <c>ToLowerInvariant</c>, never <c>ToLower</c>: the latter follows the current culture, and on
    /// a Turkish machine it would lower-case 'I' to 'ı' and break the match.
    /// </remarks>
    public static string CanonicalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var email = CanonicalizeEmail(request.Email);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            // Spend the same time a real verification would, then fail identically.
            passwordHasher.VerifyAgainstDummy(request.Password);
            throw new AuthenticationFailedException();
        }

        // The password is always verified before the account state is looked at, so a disabled
        // account cannot be distinguished from a wrong password by how long the answer takes.
        var passwordMatches = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordMatches || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        // The Admin decision: a comparison against configuration, not against the e-mail address and
        // not against the role name. The role travels in the token for future server-side
        // authorization; it plays no part here.
        var isAdmin = user.UserId == authOptions.AdminUserId;

        return new LoginResponseDto(tokenService.CreateToken(user), user.FullName, isAdmin);
    }
}
