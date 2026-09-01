using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

// TargetIndex is counted among the cards the caller could SEE. On a sprint-scoped board that is
// only some of the column, so the server translates it into a position in the whole column.
public class MoveTaskRequestDto
{
    [Range(1, int.MaxValue)]
    public int TargetColumnId { get; set; }

    [Range(0, int.MaxValue)]
    public int TargetIndex { get; set; }

    public int? SprintId { get; set; }
}
