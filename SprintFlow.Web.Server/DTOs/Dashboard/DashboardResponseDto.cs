namespace SprintFlow.Web.Server.DTOs.Dashboard;

/// <summary>
/// Everything the signed-in user's dashboard shows, in one response.
/// </summary>
/// <remarks>
/// Always the caller's own: there is no user id in the route, the query or a body, so this endpoint
/// cannot be asked for somebody else's work.
/// </remarks>
/// <param name="Counts">
/// The four buckets, over <b>all</b> of the caller's assigned tasks — including the ones
/// <paramref name="MyTasks"/> leaves out, which is how Completed and Cancelled stay visible.
/// </param>
/// <param name="MyTasks">
/// The actionable ones only: To Do and custom columns. Completed and Cancelled are deliberately
/// absent, because this list answers "what is on my plate", and the counts already report the rest.
/// </param>
/// <param name="MyTeams">
/// The boards where the caller holds at least one task, in any column. Membership is computed from
/// assignment on each read; nothing about it is stored.
/// </param>
public record DashboardResponseDto(
    DashboardTaskCountsDto Counts,
    IReadOnlyList<DashboardTaskDto> MyTasks,
    IReadOnlyList<DashboardTeamDto> MyTeams);
