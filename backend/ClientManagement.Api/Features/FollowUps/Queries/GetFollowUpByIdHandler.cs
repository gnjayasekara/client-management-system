
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Queries;

public static class GetFollowUpByIdHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var followUp = await db.FollowUps
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new FollowUpResponse(
                x.Id,
                x.ClientId,
                x.Client.Name,
                x.Title,
                x.DueDate,
                x.IsCompleted
            ))
            .FirstOrDefaultAsync(ct);

        return followUp is null
            ? Results.NotFound()
            : Results.Ok(followUp);
    }
}
