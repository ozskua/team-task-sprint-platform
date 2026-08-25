namespace SprintFlow.Web.Server.Models;

/// <summary>
/// A stretch of calendar days on one board, that tasks can be put into. Sprints on a board form a
/// single timeline: no two of them may cover the same day.
/// </summary>
/// <remarks>
/// The creator is recorded as ownership information only. It grants no permission over the sprint —
/// anyone signed in may use it — and it does not make the creator a member of the board's team.
/// <para>
/// A sprint belongs to a Board rather than to a Team because the Board is what tasks carry and what
/// the URL addresses. The two are one-to-one, so this is the same scope either way.
/// </para>
/// </remarks>
public class Sprint
{
    public int SprintId { get; set; }

    /// <summary>The owning board. Sprints are never shared between boards.</summary>
    public int BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The sprint's first day, and part of it. A whole calendar day: the column is
    /// <c>date</c>, so there is no time of day to store and no offset to shift it.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The day the sprint stops, and <b>not</b> part of it. The last day worked is the day before
    /// this one, which is what lets one sprint start on the day the previous one ends without the
    /// two overlapping.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Resolved on the server from the caller's token, never from the request body.</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// When the row was written — an audit timestamp, not one of the sprint's own dates. Supplied
    /// by the database default, so it is a <c>datetime2</c> like every other <c>CreatedDate</c>.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// The tasks put into this sprint. Restrict, not cascade — see <c>TaskItemConfiguration</c> —
    /// so a sprint still holding tasks cannot be deleted and nothing is moved for the caller.
    /// </summary>
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
