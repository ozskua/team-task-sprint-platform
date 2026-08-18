namespace SprintFlow.Web.Server.DTOs.Tasks;

/// <summary>A task as returned to a client.</summary>
/// <param name="TaskId">Identifies the task.</param>
/// <param name="Title">What the task says.</param>
/// <param name="Description">Optional detail; <c>null</c> when none was written.</param>
/// <param name="BoardId">The board this task belongs to.</param>
/// <param name="TaskColumnId">The column currently holding it — always a column of that board.</param>
/// <param name="Position">Zero-based slot within the column, decided by the server.</param>
/// <param name="CreatedByUserId">Recorded ownership. It confers no permission over the task.</param>
/// <param name="CreatedByFullName">Creator's display name, so the card can show it without a second call.</param>
/// <param name="CreatedDate">Stored without an offset, so the client renders it as-is.</param>
/// <param name="Assignee">
/// The one person responsible for the task, or <c>null</c> when it is unassigned. Separate from the
/// creator: writing a task down and being answerable for it are different things, and they are
/// routinely different people.
/// </param>
public record TaskResponseDto(
    int TaskId,
    string Title,
    string? Description,
    int BoardId,
    int TaskColumnId,
    int Position,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    TaskAssigneeResponseDto? Assignee);
