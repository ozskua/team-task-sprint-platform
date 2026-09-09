namespace SprintFlow.Web.Server.DTOs.Notifications;

// An object rather than a bare number, so the badge's endpoint can be read without knowing what the
// number means — and so it stays a shape rather than a scalar if anything is ever added beside it.
public record UnreadNotificationCountResponseDto(int UnreadCount);
