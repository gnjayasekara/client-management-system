
namespace ClientManagement.Api.Features.FollowUps.Events;

public sealed record FollowUpCreatedEvent(
    Guid EventId,
    int FollowUpId,
    int ClientId,
    DateTime OccurredAtUtc
);
