
namespace ClientManagement.Api.Features.FollowUps.Events;

public sealed record FollowUpUpdatedEvent(
    Guid EventId,
    int FollowUpId,
    int ClientId,
    DateTime OccurredAtUtc
);
