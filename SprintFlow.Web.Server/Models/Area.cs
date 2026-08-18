namespace SprintFlow.Web.Server.Models;

/// <summary>
/// The top-level domain container. Any authenticated user can create one.
/// </summary>
/// <remarks>
/// The creator is recorded as ownership information only. It grants no permission to edit, delete
/// or administer the Area, and it does not change the creator's global role.
/// </remarks>
public class Area
{
    public int AreaId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Resolved on the server from the caller's token, never from the request body.</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>Supplied by the database default — see <c>AreaConfiguration</c>.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Loaded with <c>Include(a =&gt; a.CreatedByUser)</c> when the creator's name is needed for a
    /// response.
    /// </summary>
    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// The Area's boards. Adding the first one through this collection is what lets EF insert the
    /// Area and its Board in a single save, so the two can never be created apart.
    /// </summary>
    public ICollection<Board> Boards { get; set; } = new List<Board>();
}
