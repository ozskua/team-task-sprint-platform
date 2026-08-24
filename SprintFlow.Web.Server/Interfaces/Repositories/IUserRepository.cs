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

    /// <summary>
    /// Every user with the role loaded, ordered by name. Active and inactive alike — deciding what
    /// to show is not the repository's call.
    /// </summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// How many accounts exist, active and inactive alike. An aggregate: no user is loaded, so a
    /// caller that only needs the number never pulls the directory to count it.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Whether a user with this id exists. Used before writing a foreign key to them, so the caller
    /// gets a clear rejection instead of a constraint violation surfacing as a 500.
    /// </summary>
    Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether a user with this id exists <em>and</em> can still sign in. A task assigned to a
    /// disabled account would name someone who can never open it.
    /// </summary>
    Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken);
}
