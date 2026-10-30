
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

using ClientManagement.Api.Features.FollowUps.Events;
using ClientManagement.Api.Infrastructure.Messaging.FollowUps;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class CompleteFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory,
        IFollowUpEventPublisher eventPublisher,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
            "CompleteFollowUpHandler");

        var followUp = await db.FollowUps
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (followUp is null)
            return Results.NotFound();

        if (followUp.IsCompleted)
            return Results.NoContent();

        followUp.IsCompleted = true;

        await db.SaveChangesAsync(ct);

        try
        {
            await realtimeNotifier.FollowUpChangedAsync(
                followUp.Id,
                "Completed",
                ct
            );
        }
        catch (Exception ex)
        {
            

            logger.LogWarning(
                ex,
                "Failed to broadcast FollowUpChanged for FollowUp {FollowUpId}",
                followUp.Id
            );
        }

        try
        {
            var completedEvent = new FollowUpCompletedEvent(
                Guid.NewGuid(),
                followUp.Id,
                followUp.ClientId,
                DateTime.UtcNow);

            await eventPublisher.PublishCompletedAsync(
                completedEvent,
                ct);

            logger.LogInformation(
                "Published FollowUpCompleted event for FollowUp {FollowUpId}",
                followUp.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for FollowUp {FollowUpId}",
                followUp.Id);
        }

        return Results.NoContent();
    }
}
