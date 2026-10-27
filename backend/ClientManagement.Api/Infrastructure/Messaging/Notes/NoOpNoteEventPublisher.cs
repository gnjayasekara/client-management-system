
using ClientManagement.Api.Features.Notes.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.Notes;

public sealed class NoOpNoteEventPublisher : INoteEventPublisher
{
    public Task PublishCreatedAsync(
        NoteCreatedEvent noteEvent,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishDeletedAsync(
        NoteDeletedEvent message,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
