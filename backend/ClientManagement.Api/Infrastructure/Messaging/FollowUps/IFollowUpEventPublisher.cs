
using ClientManagement.Api.Features.FollowUps.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.FollowUps;

public interface IFollowUpEventPublisher
{
    Task PublishCreatedAsync(
        FollowUpCreatedEvent message,
        CancellationToken cancellationToken);
}
