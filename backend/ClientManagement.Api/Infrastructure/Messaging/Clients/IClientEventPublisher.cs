
using ClientManagement.Api.Features.Clients.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.Clients;

public interface IClientEventPublisher
{
    Task PublishCreatedAsync(
        ClientCreatedEvent message,
        CancellationToken ct);

    Task PublishUpdatedAsync(
        ClientUpdatedEvent message,
        CancellationToken ct);

    Task PublishDeletedAsync(
        ClientDeletedEvent message,
        CancellationToken ct);
}
