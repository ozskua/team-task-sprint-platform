using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Columns;

public class ReorderColumnsRequestDto
{
    [Required]
    public IReadOnlyList<int> OrderedColumnIds { get; set; } = [];
}
