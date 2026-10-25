
using ClientManagement.Api.Features.FollowUps.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.FollowUps;

public sealed class NoOpFollowUpEventPublisher
    : IFollowUpEventPublisher
{
    public Task PublishCreatedAsync(
        FollowUpCreatedEvent message,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
