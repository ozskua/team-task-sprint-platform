namespace SprintFlow.Web.Server.Models;

/// <summary>
/// A board inside an Area. The first one is created automatically with its Area; an Area may hold
/// several.
/// </summary>
/// <remarks>
/// The creator is recorded as ownership information only — it grants no permission over the Board
/// and does not change the creator's global role.
/// </remarks>
public class Board
{
    public int BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int AreaId { get; set; }

    /// <summary>Inherited from whoever created the Area this Board was created with.</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>Supplied by the database default — see <c>BoardConfiguration</c>.</summary>
    public DateTime CreatedDate { get; set; }

    public Area Area { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// The board's one team, created with it. Assigning this before the first save is what lets EF
    /// insert both rows together.
    /// </summary>
    public Team Team { get; set; } = null!;

    /// <summary>
    /// The board's workflow columns. The three system columns are added through this collection
    /// when the Board is created, so they go in on the same save as the Board and its Team.
    /// </summary>
    public ICollection<WorkflowColumn> WorkflowColumns { get; set; } = new List<WorkflowColumn>();

    /// <summary>
    /// Every task on this board, whichever column currently holds it. Team membership is derived by
    /// filtering these, which is why tasks carry the board directly.
    /// </summary>
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
