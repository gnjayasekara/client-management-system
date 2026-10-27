
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Api.Infrastructure.Realtime;

using ClientManagement.Api.Features.FollowUps.Events;
using ClientManagement.Api.Infrastructure.Messaging.FollowUps;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class UpdateFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        UpdateFollowUpRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        IFollowUpEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var followUp = await db.FollowUps
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (followUp is null)
            return Results.NotFound();

        var clientExists = await db.Clients
            .AnyAsync(x => x.Id == request.ClientId, ct);

        if (!clientExists)
        {
            return Results.NotFound(
                new { message = "Client not found." });
        }

        followUp.ClientId = request.ClientId;
        followUp.Title = request.Title.Trim();
        followUp.DueDate = request.DueDate;
        followUp.IsCompleted = request.IsCompleted;

        await db.SaveChangesAsync(ct);

       var logger = loggerFactory.CreateLogger(
            "UpdateFollowUpHandler");

        // SignalR
        try
        {
            await realtimeNotifier.FollowUpChangedAsync(
                followUp.Id,
                "Updated",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SignalR notification failed for FollowUp {FollowUpId}",
                followUp.Id);
        }

        // RabbitMQ event
        try
        {
            var followUpUpdatedEvent = new FollowUpUpdatedEvent(
                Guid.NewGuid(),
                followUp.Id,
                followUp.ClientId,
                DateTime.UtcNow);

            await eventPublisher.PublishUpdatedAsync(
                followUpUpdatedEvent,
                ct);

            logger.LogInformation(
                "Published FollowUpUpdated event for FollowUp {FollowUpId}",
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
