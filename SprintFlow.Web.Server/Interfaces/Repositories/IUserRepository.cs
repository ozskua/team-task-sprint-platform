using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Finds a user by an already-canonicalised e-mail address, with the role loaded. Returns
    /// <c>null</c> when nothing matches — an unknown address is not an exceptional case here, it is
    /// one of the ordinary outcomes of a sign-in attempt.
    /// </summary>
    Task<User?> GetByEmailAsync(string canonicalEmail, CancellationToken cancellationToken);
}
