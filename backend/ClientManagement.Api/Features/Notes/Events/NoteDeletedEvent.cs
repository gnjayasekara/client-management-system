namespace ClientManagement.Api.Features.Notes.Events;

public sealed record NoteDeletedEvent(
    Guid EventId,
    int NoteId,
    int ClientId,
    DateTime OccurredAtUtc
);