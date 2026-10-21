
using Microsoft.AspNetCore.SignalR;

namespace ClientManagement.Api.Infrastructure.Realtime;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRRealtimeNotifier(
        IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task ClientChangedAsync(
        int clientId,
        string action,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(
            "ClientChanged",
            new { entityId = clientId, action },
            cancellationToken);
    }

    public Task FollowUpChangedAsync(
        int followUpId,
        string action,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(
            "FollowUpChanged",
            new { entityId = followUpId, action },
            cancellationToken);
    }

    public Task NoteChangedAsync(
        int clientId,
        string action,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(
            "NoteChanged",
            new { entityId = clientId, action },
            cancellationToken);
    }
}
