using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Notifications;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<NotificationResponseDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await notificationService.GetForUserAsync(
            User.GetAuthenticatedUserId(),
            cancellationToken));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnreadNotificationCountResponseDto>> GetUnreadCount(
        CancellationToken cancellationToken)
        => Ok(new UnreadNotificationCountResponseDto(
            await notificationService.GetUnreadCountAsync(
                User.GetAuthenticatedUserId(),
                cancellationToken)));

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

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(User.GetAuthenticatedUserId(), cancellationToken);

        return NoContent();
    }
}
