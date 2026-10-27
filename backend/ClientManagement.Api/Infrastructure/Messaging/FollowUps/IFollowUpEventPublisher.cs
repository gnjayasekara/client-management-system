
using ClientManagement.Api.Features.FollowUps.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.FollowUps;

public interface IFollowUpEventPublisher
{
    Task PublishCreatedAsync(
        FollowUpCreatedEvent message,
        CancellationToken ct);

    Task PublishUpdatedAsync(
        FollowUpUpdatedEvent message,
        CancellationToken ct);

    Task PublishCompletedAsync(
        FollowUpCompletedEvent message,
        CancellationToken ct);

    Task PublishDeletedAsync(
        FollowUpDeletedEvent message,
        CancellationToken ct);
}
