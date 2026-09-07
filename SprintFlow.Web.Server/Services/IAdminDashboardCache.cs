using SprintFlow.Web.Server.DTOs.AdminDashboard;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Where the system-wide dashboard figures are kept between reads.
/// </summary>
/// <remarks>
/// An optimisation over the database, never a second source of truth: either method may do nothing
/// at all - because Redis is unreachable, or nothing has been cached yet - and the caller still gets
/// the right answer by computing it.
/// <para>
/// There is deliberately no Remove: unlike the board summary, this entry is not dropped by the
/// writes that change it. It is replaced on a timer by <see cref="AdminDashboardRefreshService"/>,
/// so nothing in the application ever needs to invalidate it.
/// </para>
/// </remarks>
public interface IAdminDashboardCache
{
    /// <summary>The cached figures, or null when nothing is cached - including Redis being down.</summary>
    Task<AdminDashboardCountsDto?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(AdminDashboardCountsDto counts, CancellationToken cancellationToken);
}
