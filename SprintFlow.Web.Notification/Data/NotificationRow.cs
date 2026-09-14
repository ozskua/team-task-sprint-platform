using SprintFlow.Web.Notification.Messaging;

namespace SprintFlow.Web.Notification.Data;

/// <summary>
/// The four columns this service writes, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not the API service's rich <c>Notification</c> entity. That one carries navigation
/// properties to <c>User</c> and <c>TaskItem</c> because its read query joins them to show an
/// actor's name and a task's title; dragging those here would drag the whole domain model into a
/// service whose only job is an INSERT.
/// </para>
/// <para>
/// Named <c>Row</c> rather than <c>Notification</c> to say what it is: a write projection over the
/// same table, not a second definition of the domain concept. <b>The API service owns the schema
/// and every migration</b> — see the roadmap on database ownership, including the honest note that
/// two services sharing one table is a coupling this version accepts.
/// </para>
/// <para>
/// <c>IsRead</c> and <c>CreatedDate</c> are absent on purpose: the database supplies both by
/// column default, exactly as it did when the API service wrote these rows.
/// </para>
/// </remarks>
public class NotificationRow
{
    public int NotificationId { get; set; }

    public int RecipientUserId { get; set; }

    public int ActorUserId { get; set; }

    public NotificationType Type { get; set; }

    public int TaskId { get; set; }
}
