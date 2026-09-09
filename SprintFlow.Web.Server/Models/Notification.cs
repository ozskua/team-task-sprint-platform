using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Models;

/// <summary>
/// One thing that happened, addressed to one person.
/// </summary>
/// <remarks>
/// Every notification this project produces is about a task, so <see cref="TaskId"/> is a real
/// foreign key rather than a general-purpose reference column. There is deliberately no metadata
/// blob and no polymorphic target: a notification about something other than a task would need a
/// schema change, which is the honest cost of not inventing one now.
/// <para>
/// The sentence a reader sees is not stored. It is composed on the client from the type, the
/// actor's name and the task's title, so a task renamed after the fact does not leave a
/// notification quoting a title that no longer exists.
/// </para>
/// </remarks>
public class Notification
{
    public int NotificationId { get; set; }

    /// <summary>Who this is for. Resolved from the caller's token, never from a request body.</summary>
    public int RecipientUserId { get; set; }

    /// <summary>Who did the thing. Never the recipient — nobody is told about their own action.</summary>
    public int ActorUserId { get; set; }

    public NotificationType Type { get; set; }

    public int TaskId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }

    public User Recipient { get; set; } = null!;

    public User Actor { get; set; } = null!;

    public TaskItem Task { get; set; } = null!;
}
