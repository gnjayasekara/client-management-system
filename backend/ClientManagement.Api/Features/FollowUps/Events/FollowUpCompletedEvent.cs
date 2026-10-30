
namespace ClientManagement.Api.Features.FollowUps.Events;

public sealed record FollowUpCompletedEvent(
    Guid EventId,
    int FollowUpId,
    int ClientId,
    DateTime OccurredAtUtc
);
