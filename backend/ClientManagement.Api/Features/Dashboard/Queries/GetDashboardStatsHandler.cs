
using System.Text.Json;
using ClientManagement.Api.Features.Dashboard.DTOs;
using ClientManagement.Api.Infrastructure.Caching;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ClientManagement.Api.Features.Dashboard.Queries;

public static class GetDashboardStatsHandler
{
    public static async Task<IResult> HandleAsync(
        AppDbContext db,
        IDistributedCache cache,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
            "GetDashboardStatsHandler");

        var key = DashboardCacheKeys.Stats;

        // 1. Attempt to retrieve dashboard statistics
        try
        {
            var cachedJson = await cache.GetStringAsync(key, ct);

            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer
                    .Deserialize<DashboardStatsResponse>(cachedJson);

                if (cached is not null)
                {
                    logger.LogInformation(
                        "Dashboard Redis cache HIT");

                    return Results.Ok(cached);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Dashboard cache read failed. Falling back to SQL Server.");
        }

        logger.LogInformation(
            "Dashboard Redis cache MISS");

        // 2. Original database queries
        var totalClients = await db.Clients.CountAsync(ct);

        var activeClients = await db.Clients
            .CountAsync(x => x.IsActive, ct);

        var inactiveClients = totalClients - activeClients;

        var pendingFollowUps = await db.FollowUps
            .CountAsync(x => !x.IsCompleted, ct);

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

        
        try
        {
            await cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(5)
                },
                ct
            );

            logger.LogInformation(
                "Dashboard statistics cached for 5 minutes");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Dashboard cache write failed. Returning SQL Server result.");
        }

        return Results.Ok(response);
    }
}
