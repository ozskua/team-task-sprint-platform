using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Columns;

/// <summary>The new name for a custom column. System columns cannot be renamed.</summary>
public class RenameWorkflowColumnRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
