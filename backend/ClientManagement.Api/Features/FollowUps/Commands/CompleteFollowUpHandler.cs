
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
        CancellationToken ct)
    {
        var followUp = await db.FollowUps
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (followUp is null)
            return Results.NotFound();

        followUp.IsCompleted = true;

        await db.SaveChangesAsync(ct);

        await realtimeNotifier.FollowUpChangedAsync(
            followUp.Id,
            "Completed",
            ct
        );

        return Results.NoContent();
    }
}
