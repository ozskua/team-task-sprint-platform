using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Columns;

/// <summary>
/// The board's custom columns in the order the caller wants them.
/// </summary>
/// <remarks>
/// System columns are not part of this payload and naming one is an error: To Do stays first,
/// Completed and Cancelled stay last, and the server puts them there whatever it is sent. The list
/// must be exactly the board's custom columns — nothing missing, nothing repeated, nothing foreign.
/// </remarks>
public class ReorderColumnsRequestDto
{
    [Required]
    public IReadOnlyList<int> OrderedColumnIds { get; set; } = [];
}
