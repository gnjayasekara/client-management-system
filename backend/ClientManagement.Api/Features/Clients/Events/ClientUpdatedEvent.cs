
namespace ClientManagement.Api.Features.Clients.Events;

public sealed record ClientUpdatedEvent(
    Guid EventId,
    int ClientId,
    string Name,
    DateTime OccurredAtUtc
);
