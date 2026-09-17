using Microsoft.AspNetCore.SignalR;

namespace SprintFlow.Web.Server.Hubs

{
    public class TestHub : Hub
    {
        public static string GroupFor(int boardId) => $"board:{boardId}";

        public Task JoinBoard(int boardId)
            => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(boardId));

    }
}
