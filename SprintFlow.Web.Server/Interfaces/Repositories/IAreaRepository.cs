using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

public interface IAreaRepository
{
    /// <summary>
    /// Persists a new Area and returns its generated id. Saving happens here because the project
    /// has no unit of work — a single insert needs no transaction spanning several repositories.
    /// </summary>
    Task<int> AddAsync(Area area, CancellationToken cancellationToken);

    /// <summary>The Area with its creator loaded, or <c>null</c> if no such Area exists.</summary>
    Task<Area?> GetByIdAsync(int areaId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the Area exists. Cheaper than loading it, and it is all the Board flow needs — any
    /// authenticated user may add a board to any Area, so there is no ownership to inspect.
    /// </summary>
    Task<bool> ExistsAsync(int areaId, CancellationToken cancellationToken);

    /// <summary>
    /// Every Area, with creators loaded. There is no Area membership and no scoped authorization
    /// in the domain model, so there is nothing to filter by.
    /// </summary>
    Task<IReadOnlyList<Area>> GetAllAsync(CancellationToken cancellationToken);
}
