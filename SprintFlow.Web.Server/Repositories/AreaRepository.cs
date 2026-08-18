using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class AreaRepository : IAreaRepository
{
    private readonly SprintFlowDbContext context;

    public AreaRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public async Task<int> AddAsync(Area area, CancellationToken cancellationToken)
    {
        context.Areas.Add(area);

        // One SaveChanges for the Area and everything hanging off it. EF wraps a single save in a
        // transaction, so the Area and its first Board are committed together or not at all — there
        // is no state where one exists without the other.
        //
        // CreatedDate is left untouched on both: the column defaults supply them, and EF reads the
        // generated values back along with the new keys.
        await context.SaveChangesAsync(cancellationToken);

        return area.AreaId;
    }

    public Task<Area?> GetByIdAsync(int areaId, CancellationToken cancellationToken)
        => WithRelations()
            .FirstOrDefaultAsync(a => a.AreaId == areaId, cancellationToken);

    public Task<bool> ExistsAsync(int areaId, CancellationToken cancellationToken)
        => context.Areas.AnyAsync(a => a.AreaId == areaId, cancellationToken);

    public async Task<IReadOnlyList<Area>> GetAllAsync(CancellationToken cancellationToken)
        => await WithRelations()
            .OrderByDescending(a => a.CreatedDate) // Newest first; ties broken by insertion order.
            .ThenByDescending(a => a.AreaId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Everything a response needs: the Area's creator, its boards, and each board's creator.
    /// </summary>
    private IQueryable<Area> WithRelations()
        => context.Areas
            .AsNoTracking()
            .Include(a => a.CreatedByUser)
            .Include(a => a.Boards)
                .ThenInclude(b => b.CreatedByUser);
}
