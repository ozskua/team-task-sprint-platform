using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Sprints;

/// <summary>
/// Everything a caller may supply when creating a sprint: what it is called, and which days it
/// covers.
/// </summary>
/// <remarks>
/// There is deliberately no <c>CreatedByUserId</c> and no <c>BoardId</c>. The creator comes from the
/// caller's token and the board comes from the route, so a client that sends either is ignored by
/// the model binder rather than rejected by a check someone could forget to write.
/// <para>
/// Both dates are <see cref="DateOnly"/>, so a value carrying a time of day fails to bind and never
/// reaches the service. There is nothing to strip and nothing to normalise.
/// </para>
/// </remarks>
public class CreateSprintRequestDto
{
    // [Required] also rejects a whitespace-only name: RequiredAttribute trims before testing unless
    // AllowEmptyStrings is set. MaxLength matches the column width.
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The sprint's first day, and part of it.</summary>
    [Required]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The day the sprint stops, and <b>not</b> part of it. It must be after
    /// <see cref="StartDate"/>; equal dates would describe a sprint covering no days at all.
    /// </summary>
    [Required]
    public DateOnly EndDate { get; set; }
}
