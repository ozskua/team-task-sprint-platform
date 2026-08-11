using SprintFlow.Web.Server.DTOs.Auth;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IAuthService
{
    /// <summary>
    /// Authenticates a sign-in attempt. Throws
    /// <see cref="Common.Exceptions.AuthenticationFailedException"/> for every failure, with no
    /// indication of which check failed.
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}
