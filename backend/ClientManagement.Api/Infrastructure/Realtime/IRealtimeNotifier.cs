
namespace ClientManagement.Api.Infrastructure.Realtime;

public interface IRealtimeNotifier
{
    Task ClientChangedAsync(
        int clientId,
        string action,
        CancellationToken cancellationToken = default);

    Task FollowUpChangedAsync(
        int followUpId,
        string action,
        CancellationToken cancellationToken = default);

    Task NoteChangedAsync(
        int clientId,
        string action,
        CancellationToken cancellationToken = default);
}
