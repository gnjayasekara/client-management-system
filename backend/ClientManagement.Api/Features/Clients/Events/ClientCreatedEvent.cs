namespace ClientManagement.Api.Features.Clients.Events;

public sealed record ClientCreatedEvent(
    Guid EventId,
    int ClientId,
    string Name,
    DateTime OccurredAtUtc
);