using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SprintFlow.Web.SignalR.Hubs;

/// <summary>
/// Tells one person that they have a new notification. Nothing more.
/// </summary>
/// <remarks>
/// Deliberately empty, and that is the whole design: unlike <see cref="BoardHub"/>, there is no
/// group to join. SignalR addresses a person directly through <c>Clients.User(id)</c>, and its
/// default provider reads <c>ClaimTypes.NameIdentifier</c> from the connection's token — the claim
/// the API service writes when it issues the token and the claim this service is configured to read
/// back. So a client connects and is reachable; there is no membership call to make.
/// <para>
/// Like the board hub, this announces rather than carries: the event has no payload, and the client
/// answers it by re-reading over REST from the API service, which stays the only source of truth.
/// </para>
/// </remarks>
[Authorize]
public class NotificationHub : Hub
{
}
