
namespace ClientManagement.Api.Features.Clients.Events;

public sealed record ClientDeletedEvent(
    Guid EventId,
    int ClientId,
    string Name,
    DateTime OccurredAtUtc
);
