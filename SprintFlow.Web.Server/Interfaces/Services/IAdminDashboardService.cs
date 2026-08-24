using SprintFlow.Web.Server.DTOs.AdminDashboard;

namespace SprintFlow.Web.Server.Interfaces.Services;

/// <summary>
/// Builds the admin overview: the state of the system, and every task in it.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>
    /// The whole system's dashboard. Never returns <c>null</c> — an empty database gets zero counts
    /// and an empty list, which is a correct answer rather than a missing one.
    /// </summary>
    /// <remarks>
    /// Takes no user id: this view is the same for every caller. It is deliberately not the mirror
    /// of <c>IDashboardService</c>, which answers "what is mine".
    /// </remarks>
    Task<AdminDashboardResponseDto> GetAsync(CancellationToken cancellationToken);
}
