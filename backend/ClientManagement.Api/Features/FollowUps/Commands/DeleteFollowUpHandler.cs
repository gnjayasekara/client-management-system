
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class DeleteFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var followUp = await db.FollowUps
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (followUp is null)
            return Results.NotFound();

        db.FollowUps.Remove(followUp);

        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
