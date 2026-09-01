using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Sprints;

// [StartDate, EndDate) - the end day is NOT part of the sprint, which is what lets one sprint
// start on the day the previous one ends.
public class CreateSprintRequestDto
{
    // [Required] also rejects a whitespace-only name: RequiredAttribute trims before testing unless
    // AllowEmptyStrings is set. MaxLength matches the column width.
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}
