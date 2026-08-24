namespace SprintFlow.Web.Server.DTOs.AdminDashboard;

/// <summary>
/// The admin overview: the state of the system, and every task in it.
/// </summary>
/// <remarks>
/// System-wide and identical for every caller — unlike <c>GET /api/dashboard</c>, which answers
/// "what is mine". Deliberately carries no user directory: the Users page already lists everyone,
/// so repeating it here would duplicate a screen that exists. Only the user <em>count</em> travels,
/// inside <paramref name="Counts"/>.
/// </remarks>
/// <param name="Counts">The six summary figures, over every task, user and board.</param>
/// <param name="Tasks">
/// Every task in every board, newest first. Completed and Cancelled included — this answers "what
/// exists", not "what is left to do", so hiding finished work would make it a worse overview.
/// </param>
public record AdminDashboardResponseDto(
    AdminDashboardCountsDto Counts,
    IReadOnlyList<AdminDashboardTaskDto> Tasks);
