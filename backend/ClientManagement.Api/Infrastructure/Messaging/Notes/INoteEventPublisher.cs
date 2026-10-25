using ClientManagement.Api.Features.Notes.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.Notes;

public interface INoteEventPublisher
{
    Task PublishCreatedAsync(
        NoteCreatedEvent noteEvent,
        CancellationToken cancellationToken);
}
