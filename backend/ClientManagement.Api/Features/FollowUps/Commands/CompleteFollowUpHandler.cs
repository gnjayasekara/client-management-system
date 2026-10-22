
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class CompleteFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var followUp = await db.FollowUps
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (followUp is null)
            return Results.NotFound();

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
            var logger = loggerFactory.CreateLogger(
                "CompleteFollowUpHandler"
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
