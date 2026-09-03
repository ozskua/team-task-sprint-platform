using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SprintFlow.Web.Server.Hubs;

/// <summary>
/// Tells everyone looking at a board that it has changed. Nothing more.
/// </summary>
/// <remarks>
/// Deliberately thin: it owns group membership and no business rule. Every write still goes through
/// the REST API and the services behind it, which stay the source of truth; this only says that
/// something happened, so the other clients know to re-read.
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
