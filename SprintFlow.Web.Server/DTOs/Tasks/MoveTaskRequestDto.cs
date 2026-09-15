using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

public class MoveTaskRequestDto
{
    [Range(1, int.MaxValue)]
    public int TargetColumnId { get; set; }

    [Range(0, int.MaxValue)]
    public int TargetIndex { get; set; }

    public int? SprintId { get; set; }
}
