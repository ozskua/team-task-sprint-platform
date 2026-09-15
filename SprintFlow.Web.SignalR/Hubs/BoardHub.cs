using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SprintFlow.Web.SignalR.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public static string GroupFor(int boardId) => $"board:{boardId}";

    public Task JoinBoard(int boardId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(boardId));

    public Task LeaveBoard(int boardId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(boardId));
}
