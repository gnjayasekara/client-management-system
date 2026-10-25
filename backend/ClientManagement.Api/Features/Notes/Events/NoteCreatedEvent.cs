
namespace ClientManagement.Api.Features.Notes.Events;

public sealed record NoteCreatedEvent(
    Guid EventId,
    int NoteId,
    int ClientId,
    DateTime OccurredAtUtc
);