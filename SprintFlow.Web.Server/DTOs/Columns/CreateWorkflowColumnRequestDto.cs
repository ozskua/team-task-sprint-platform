using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Columns;

public class CreateWorkflowColumnRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
