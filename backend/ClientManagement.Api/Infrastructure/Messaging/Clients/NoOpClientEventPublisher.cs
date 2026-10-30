
using ClientManagement.Api.Features.Clients.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.Clients;

public sealed class NoOpClientEventPublisher
    : IClientEventPublisher
{
    public Task PublishCreatedAsync(
        ClientCreatedEvent message,
        CancellationToken ct) =>
        Task.CompletedTask;

    public Task PublishUpdatedAsync(
        ClientUpdatedEvent message,
        CancellationToken ct) =>
        Task.CompletedTask;

    public Task PublishDeletedAsync(
        ClientDeletedEvent message,
        CancellationToken ct) =>
        Task.CompletedTask;
}
