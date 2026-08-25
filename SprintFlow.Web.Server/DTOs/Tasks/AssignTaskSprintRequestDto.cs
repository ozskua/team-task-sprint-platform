namespace SprintFlow.Web.Server.DTOs.Tasks;

/// <summary>
/// Which sprint a task belongs to. The task comes from the route; this says where to put it.
/// </summary>
/// <remarks>
/// One property, deliberately: putting a task into a sprint and taking it out again are the same
/// operation with a different value, exactly as replacing and clearing an assignee are.
/// </remarks>
public class AssignTaskSprintRequestDto
{
    /// <summary>
    /// The sprint to put the task into, or <c>null</c> to take it out of whichever one it is in. A
    /// named sprint must exist and belong to the same board as the task.
    /// </summary>
    public int? SprintId { get; set; }
}
