using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Sprints;

public class CreateSprintRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}
