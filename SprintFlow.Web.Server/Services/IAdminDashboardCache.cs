using SprintFlow.Web.Server.DTOs.AdminDashboard;

namespace SprintFlow.Web.Server.Services;

public interface IAdminDashboardCache
{
    Task<AdminDashboardCountsDto?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(AdminDashboardCountsDto counts, CancellationToken cancellationToken);
}
