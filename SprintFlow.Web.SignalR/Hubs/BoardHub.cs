using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SprintFlow.Web.SignalR.Hubs;

/// <summary>
/// Tells everyone looking at a board that it has changed. Nothing more.
/// </summary>
/// <remarks>
/// Moved here from the API service unchanged: it owns group membership and no business rule. Every
/// write still goes through the REST API, which stays the source of truth; this only says that
/// something happened, so the other clients know to re-read.
/// <para>
/// The two methods are what the browser calls; what the browser is <em>told</em> now arrives over
/// RabbitMQ instead of from an in-process service call. The client-facing contract — the method
/// names, the group name and the <c>boardChanged</c> event — is deliberately identical, so no
/// Angular file had to change.
/// </para>
/// <para>
/// SignalR removes a connection from its groups when it disconnects, so there is nothing to clean
/// up here.
/// </para>
/// </remarks>
[Authorize]
public class BoardHub : Hub
{
    /// <summary>The group every client watching one board belongs to.</summary>
    public static string GroupFor(int boardId) => $"board:{boardId}";

    public Task JoinBoard(int boardId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(boardId));

    public Task LeaveBoard(int boardId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(boardId));
}
