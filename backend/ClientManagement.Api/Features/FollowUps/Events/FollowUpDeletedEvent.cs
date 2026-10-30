
namespace ClientManagement.Api.Features.FollowUps.Events;

public sealed record FollowUpDeletedEvent(
    Guid EventId,
    int FollowUpId,
    int ClientId,
    DateTime OccurredAtUtc
);
