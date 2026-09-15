using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Auth;
using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

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

    public static string CanonicalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var email = CanonicalizeEmail(request.Email);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            passwordHasher.VerifyAgainstDummy(request.Password);
            throw new AuthenticationFailedException();
        }

        var passwordMatches = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordMatches || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        var isAdmin = user.UserId == authOptions.AdminUserId;

        return new LoginResponseDto(user.UserId, tokenService.CreateToken(user), user.FullName, isAdmin);
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users.Select(user => user.ToListItemDto()).ToList();
    }
}
