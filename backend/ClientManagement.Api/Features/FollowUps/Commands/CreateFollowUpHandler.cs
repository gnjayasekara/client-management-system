
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class CreateFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        CreateFollowUpRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
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

        // Save the follow-up before broadcasting
        await db.SaveChangesAsync(ct);

        // Notify other connected users
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
            var logger = loggerFactory.CreateLogger(
                "CreateFollowUpHandler"
            );

            logger.LogWarning(
                ex,
                "Failed to broadcast FollowUpChanged for FollowUp {FollowUpId}",
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
