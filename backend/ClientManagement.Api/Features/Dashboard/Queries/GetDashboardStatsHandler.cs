
using ClientManagement.Api.Features.Dashboard.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Dashboard.Queries;

public static class GetDashboardStatsHandler
{
    public static async Task<IResult> HandleAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        // Count all clients
        var totalClients = await db.Clients
            .CountAsync(ct);

        // Count active clients
        var activeClients = await db.Clients
            .CountAsync(x => x.IsActive, ct);

        // Calculate inactive clients
        var inactiveClients = totalClients - activeClients;

        // Count pending follow-ups
        var pendingFollowUps = await db.FollowUps
            .CountAsync(x => !x.IsCompleted, ct);

        // Retrieve the 5 most recent clients
        var recentClients = await db.Clients
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(5)
            .Select(x => new RecentClientResponse(
                x.Id,
                x.Name,
                x.Company,
                x.Email,
                x.IsActive,
                x.CreatedAt
            ))
            .ToListAsync(ct);

        var response = new DashboardStatsResponse(
            totalClients,
            activeClients,
            inactiveClients,
            pendingFollowUps,
            recentClients
        );

        return Results.Ok(response);
    }
}
