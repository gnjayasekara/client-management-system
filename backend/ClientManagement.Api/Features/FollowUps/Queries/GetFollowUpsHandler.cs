
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Queries;

public static class GetFollowUpsHandler
{
    public static async Task<IResult> HandleAsync(
        int? clientId,
        bool? isCompleted,
        AppDbContext db,
        CancellationToken ct)
    {
        var query = db.FollowUps.AsNoTracking();

        if (clientId.HasValue)
        {
            query = query.Where(x =>
                x.ClientId == clientId.Value);
        }

        if (isCompleted.HasValue)
        {
            query = query.Where(x =>
                x.IsCompleted == isCompleted.Value);
        }

        var followUps = await query
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Id)
            .Select(x => new FollowUpResponse(
                x.Id,
                x.ClientId,
                x.Client.Name,
                x.Title,
                x.DueDate,
                x.IsCompleted
            ))
            .ToListAsync(ct);

        return Results.Ok(followUps);
    }
}
