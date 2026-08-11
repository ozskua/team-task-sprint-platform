using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Authentication;

public interface ITokenService
{
    /// <summary>
    /// Builds a signed JWT for a successfully authenticated user. The user must have been loaded
    /// with its <see cref="User.Role"/>, because the role claim carries the role name.
    /// </summary>
    string CreateToken(User user);
}
