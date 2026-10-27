
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Api.Infrastructure.Realtime;

using ClientManagement.Api.Features.FollowUps.Events;
using ClientManagement.Api.Infrastructure.Messaging.FollowUps;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class DeleteFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory,
        IFollowUpEventPublisher eventPublisher,
        CancellationToken ct)
    {
        var followUp = await db.FollowUps
            .FirstOrDefaultAsync(x => x.Id == id, ct);

       
        if (followUp is null)
            return Results.NotFound();

        
        var followUpId = followUp.Id;
        var clientId = followUp.ClientId;

        db.FollowUps.Remove(followUp);

        await db.SaveChangesAsync(ct);

        var logger = loggerFactory.CreateLogger(
            "DeleteFollowUpHandler");

        // SignalR
        try
        {
            await realtimeNotifier.FollowUpChangedAsync(
                followUpId,
                "Deleted",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SignalR notification failed for FollowUp {FollowUpId}",
                followUpId);
        }

        // RabbitMQ event
        try
        {
            var followUpDeletedEvent = new FollowUpDeletedEvent(
                Guid.NewGuid(),
                followUpId,
                clientId,
                DateTime.UtcNow);

            await eventPublisher.PublishDeletedAsync(
                followUpDeletedEvent,
                ct);

            logger.LogInformation(
                "Published FollowUpDeleted event for FollowUp {FollowUpId}",
                followUpId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for deleted FollowUp {FollowUpId}",
                followUpId);
        }

        return Results.NoContent();

    }
}
