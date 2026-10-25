
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Features.FollowUps.Events;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Messaging.FollowUps;
using ClientManagement.Api.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class CreateFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        CreateFollowUpRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        IFollowUpEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
            "CreateFollowUpHandler");

        // Find the client and retrieve its name
        var clientName = await db.Clients
            .Where(x => x.Id == request.ClientId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);

        if (clientName is null)
        {
            return Results.NotFound(new
            {
                message = "Client not found."
            });
        }

        var followUp = new FollowUp
        {
            ClientId = request.ClientId,
            Title = request.Title.Trim(),
            DueDate = request.DueDate,
            IsCompleted = false
        };

        db.FollowUps.Add(followUp);

        // Save to SQL 
        await db.SaveChangesAsync(ct);

        // SignalR functionality
        try
        {
            await realtimeNotifier.FollowUpChangedAsync(
                followUp.Id,
                "Created",
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

        // RabbitMQ event publishing
        var followUpCreatedEvent = new FollowUpCreatedEvent(
            Guid.NewGuid(),
            followUp.Id,
            followUp.ClientId,
            DateTime.UtcNow
        );

        try
        {
            await eventPublisher.PublishCreatedAsync(
                followUpCreatedEvent,
                ct
            );

            logger.LogInformation(
                "Published FollowUpCreated event {EventId} for FollowUp {FollowUpId}",
                followUpCreatedEvent.EventId,
                followUp.Id
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for FollowUp {FollowUpId}",
                followUp.Id
            );
        }

        var response = new FollowUpResponse(
            followUp.Id,
            followUp.ClientId,
            clientName,
            followUp.Title,
            followUp.DueDate,
            followUp.IsCompleted
        );

        return Results.Created(
            $"/api/followups/{followUp.Id}",
            response
        );
    }
}
