using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Notifications;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// The caller's own notifications. There is no route, query string or body anywhere here that names
/// a user: every endpoint reads the recipient from the validated token, so a client cannot ask for
/// anyone else's notifications and cannot change the read state of one.
/// </summary>
/// <remarks>
/// Someone else's notification answers 404 rather than 403, exactly as another task's comment does:
/// a caller learns nothing about ids they do not own. No policy, role or guard is introduced —
/// being the recipient is a filter on the query, not an authorization system.
/// </remarks>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        this.notificationService = notificationService;
    }

    /// <summary>The caller's most recent notifications, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<NotificationResponseDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await notificationService.GetForUserAsync(
            User.GetAuthenticatedUserId(),
            cancellationToken));

    /// <summary>What the badge shows. Counted by the database.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnreadNotificationCountResponseDto>> GetUnreadCount(
        CancellationToken cancellationToken)
        => Ok(new UnreadNotificationCountResponseDto(
            await notificationService.GetUnreadCountAsync(
                User.GetAuthenticatedUserId(),
                cancellationToken)));

    /// <summary>Marks one of the caller's notifications read.</summary>
    [HttpPut("{notificationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int notificationId, CancellationToken cancellationToken)
        => await notificationService.MarkReadAsync(
            notificationId,
            User.GetAuthenticatedUserId(),
            cancellationToken)
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Notification not found",
                detail: "No notification exists with the requested id.");

    /// <summary>Clears the badge: everything the caller has becomes read, in one update.</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(User.GetAuthenticatedUserId(), cancellationToken);

        return NoContent();
    }
}
