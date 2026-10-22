
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Api.Infrastructure.Realtime;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class UpdateFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        UpdateFollowUpRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
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

        try
        {
            await realtimeNotifier.FollowUpChangedAsync(
                followUp.Id,
                "Updated",
                ct
            );
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(
                "UpdateFollowUpHandler"
            );

            logger.LogWarning(
                ex,
                "Failed to broadcast FollowUpChanged for FollowUp {FollowUpId}",
                followUp.Id
            );
        }

        return Results.NoContent();
    }
}
