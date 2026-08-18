namespace SprintFlow.Web.Server.DTOs.Tasks;

/// <summary>
/// The one person a task is assigned to. Absent from a task response when nobody is.
/// </summary>
/// <remarks>
/// Distinct from the task's creator, which is a separate pair of fields on the response: one says
/// who wrote the task down, this says who is responsible for doing it.
/// </remarks>
/// <param name="UserId">Identifies the assignee.</param>
/// <param name="FullName">Display name, composed on the server.</param>
public record TaskAssigneeResponseDto(
    int UserId,
    string FullName);
