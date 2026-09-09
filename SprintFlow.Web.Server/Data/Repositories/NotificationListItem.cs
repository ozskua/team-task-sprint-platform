using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Data.Repositories;

/// <summary>
/// One row of the notification list, as the database returns it.
/// </summary>
/// <remarks>
/// A projection rather than the entity, because reading the actor through <c>Include</c> would
/// bring every column of <c>Users</c> — <c>PasswordHash</c> among them — into memory to show a
/// name. Same reasoning as <see cref="TaskColumnKindCount"/>: the repository returns exactly what
/// was asked of the database, and the mapping turns it into what leaves the server.
/// </remarks>
public sealed record NotificationListItem(
    int NotificationId,
    NotificationType Type,
    int ActorUserId,
    string ActorFirstName,
    string ActorLastName,
    int TaskId,
    string TaskTitle,
    int BoardId,
    bool IsRead,
    DateTime CreatedDate);
