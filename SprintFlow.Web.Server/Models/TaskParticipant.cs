namespace SprintFlow.Web.Server.Models;

public class TaskParticipant
{
    public int TaskId { get; set; }

    public int UserId { get; set; }

    public DateTime AddedDate { get; set; }

    public TaskItem Task { get; set; } = null!;

    public User User { get; set; } = null!;
}
