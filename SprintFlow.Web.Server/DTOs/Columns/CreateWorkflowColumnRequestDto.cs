using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Columns;

/// <summary>
/// Everything a caller may supply when adding a column — the name, and nothing else.
/// </summary>
/// <remarks>
/// Where the column lands is not negotiable: a new column is always custom, and always goes
/// immediately before Completed. The board comes from the route.
/// </remarks>
public class CreateWorkflowColumnRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
