using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Authentication;

public interface ITokenService
{
    string CreateToken(User user);
}
