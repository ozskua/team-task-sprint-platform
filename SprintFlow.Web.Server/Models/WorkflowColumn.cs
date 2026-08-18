using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Models;

/// <summary>
/// A column on a board's kanban view. Each board has its own columns: the three system columns it
/// is created with, plus any custom columns a user adds.
/// </summary>
/// <remarks>
/// One table with a nullable discriminator rather than separate system and custom tables — the two
/// differ only in which operations they allow, not in what they store.
/// <para>
/// The board's order is always <c>To Do</c>, then the custom columns, then <c>Completed</c>, then
/// <c>Cancelled</c>. Because the system columns are pinned, the server can always rebuild the whole
/// order from that rule; the only thing a user controls is how the custom columns sit relative to
/// one another.
/// </para>
/// </remarks>
public class WorkflowColumn
{
    public int WorkflowColumnId { get; set; }

    public int BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Zero-based, contiguous across the board. Recomputed by the server, never sent by a client.</summary>
    public int Position { get; set; }

    /// <summary>
    /// <c>null</c> means the user created this column. A non-null kind marks one of the three
    /// required columns, which can be neither renamed, deleted nor reordered.
    /// </summary>
    public SystemColumnKind? SystemKind { get; set; }

    /// <summary>Supplied by the database default — see <c>WorkflowColumnConfiguration</c>.</summary>
    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
