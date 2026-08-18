namespace SprintFlow.Web.Server.Models;

/// <summary>
/// The one user a task is assigned to. This is the only place membership of a board's team is
/// stored: a user belongs to a team exactly when they are assigned to at least one task on that
/// team's board, and that is worked out at read time rather than kept in a second table.
/// </summary>
/// <remarks>
/// A task has zero rows here when it is unassigned and one when it is assigned. The key is the
/// pair, so the same user cannot be added twice; a unique index on <see cref="TaskId"/> alone stops
/// a second, different user being added — so "at most one assignee" holds in the database and there
/// is no separate assignee column to keep in step with it.
/// </remarks>
public class TaskParticipant
{
    public int TaskId { get; set; }

    public int UserId { get; set; }

    /// <summary>Supplied by the database default — see <c>TaskParticipantConfiguration</c>.</summary>
    public DateTime AddedDate { get; set; }

    public TaskItem Task { get; set; } = null!;

    public User User { get; set; } = null!;
}
