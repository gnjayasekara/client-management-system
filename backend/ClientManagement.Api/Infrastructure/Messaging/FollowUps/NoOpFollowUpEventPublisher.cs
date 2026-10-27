
using ClientManagement.Api.Features.FollowUps.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.FollowUps;

public sealed class NoOpFollowUpEventPublisher
    : IFollowUpEventPublisher
{
    public Task PublishCreatedAsync(
        FollowUpCreatedEvent message,
        CancellationToken ct) => Task.CompletedTask;

    public Task PublishUpdatedAsync(
        FollowUpUpdatedEvent message,
        CancellationToken ct) => Task.CompletedTask;

    public Task PublishCompletedAsync(
        FollowUpCompletedEvent message,
        CancellationToken ct) => Task.CompletedTask;

    public Task PublishDeletedAsync(
        FollowUpDeletedEvent message,
        CancellationToken ct) => Task.CompletedTask;
}
