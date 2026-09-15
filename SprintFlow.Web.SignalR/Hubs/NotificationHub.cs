using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SprintFlow.Web.SignalR.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
