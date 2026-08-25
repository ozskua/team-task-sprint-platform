using SprintFlow.Web.Server.DTOs.Dashboard;

namespace SprintFlow.Web.Server.Interfaces.Services;

/// <summary>
/// Builds one user's dashboard: the tasks assigned to them and the teams that follow from those
/// assignments.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// The dashboard for one user. Never returns <c>null</c> — a user with nothing assigned gets
    /// zero counts and two empty lists, which is a correct answer rather than a missing one.
    /// </summary>
    /// <param name="userId">
    /// The caller, resolved from their token by the controller. It is never read from a request.
    /// </param>
    Task<DashboardResponseDto> GetForUserAsync(int userId, CancellationToken cancellationToken);
}
