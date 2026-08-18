namespace SprintFlow.Web.Server.Models;

/// <summary>
/// A card on a board. Named <c>TaskItem</c> rather than <c>Task</c> because every asynchronous
/// signature in this project returns <see cref="System.Threading.Tasks.Task"/>; a domain type with
/// that name would make each one ambiguous. The table is still called <c>Tasks</c>.
/// </summary>
/// <remarks>
/// The creator is recorded as ownership information only. It grants no permission over the task —
/// anyone signed in may edit or delete it — and it does not make the creator a participant or a
/// member of the board's team.
/// </remarks>
public class TaskItem
{
    public int TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Optional. Stored as <c>null</c> rather than an empty string when nothing was written.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The owning board. Held alongside <see cref="TaskColumnId"/> on purpose: board-scoped queries
    /// read it directly instead of joining through columns, and it is what lets the column
    /// relationship be keyed on the board as well — see <c>TaskItemConfiguration</c>.
    /// </summary>
    public int BoardId { get; set; }

    /// <summary>The column currently holding this task. Always a column of <see cref="BoardId"/>.</summary>
    public int TaskColumnId { get; set; }

    /// <summary>Zero-based, contiguous within the column. Recomputed by the server, never sent by a client.</summary>
    public int Position { get; set; }

    /// <summary>Resolved on the server from the caller's token, never from the request body.</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>Supplied by the database default — see <c>TaskItemConfiguration</c>.</summary>
    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public WorkflowColumn Column { get; set; } = null!;

    /// <summary>
    /// Loaded with <c>Include(t =&gt; t.CreatedByUser)</c> whenever a response carries the creator's
    /// name, which is every task response.
    /// </summary>
    public User CreatedByUser { get; set; } = null!;

    public ICollection<TaskParticipant> Participants { get; set; } = new List<TaskParticipant>();
}
