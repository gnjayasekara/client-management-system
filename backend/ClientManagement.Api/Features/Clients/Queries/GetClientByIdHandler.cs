
using System.Text.Json;
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Caching;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ClientManagement.Api.Features.Clients.Queries;

public static class GetClientByIdHandler
{
    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(2);

    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        IDistributedCache cache,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
            "GetClientByIdHandler");

        string? cacheKey = null;

        // Step 1: Try Redis.
        try
        {
            var version = await cache.GetStringAsync(
                ClientCacheKeys.Version, ct) ?? "initial";

            cacheKey = ClientCacheKeys.ById(version, id);

            var cachedJson = await cache.GetStringAsync(
                cacheKey, ct);

            if (cachedJson is not null)
            {
                var cachedClient =
                    JsonSerializer.Deserialize<ClientResponse>(
                        cachedJson);

                if (cachedClient is not null)
                {
                    logger.LogInformation(
                        "Client by ID cache HIT: {CacheKey}",
                        cacheKey);

                    return Results.Ok(cachedClient);
                }
            }

            logger.LogInformation(
                "Client by ID cache MISS: {CacheKey}",
                cacheKey);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Redis lookup failed for Client {ClientId}. Falling back to SQL Server.",
                id);

            cacheKey = null;
        }

        // Step 2: Query SQL Server.
        var client = await db.Clients
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ClientResponse(
                x.Id,
                x.Name,
                x.Company,
                x.Email,
                x.Phone,
                x.Address,
                x.IsActive,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);

        if (client is null)
            return Results.NotFound();

        // Step 3: Cache the existing Client.
        if (cacheKey is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(client);

                await cache.SetStringAsync(
                    cacheKey,
                    json,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            CacheDuration
                    },
                    ct);

                logger.LogInformation(
                    "Client by ID CACHED: {CacheKey}",
                    cacheKey);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to cache Client {ClientId}",
                    id);
            }
        }

        return Results.Ok(client);
    }
}
