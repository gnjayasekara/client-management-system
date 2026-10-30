
using System.Text.Json;
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Caching;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ClientManagement.Api.Features.Clients.Queries;

public static class GetClientsHandler
{
    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(2);

    public static async Task<IResult> HandleAsync(
        string? search,
        bool? isActive,
        AppDbContext db,
        IDistributedCache cache,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
            "GetClientsHandler");

        string? cacheKey = null;

        // Step 1: Try Redis.
        try
        {
            var version = await cache.GetStringAsync(
                ClientCacheKeys.Version, ct) ?? "initial";

            cacheKey = ClientCacheKeys.List(
                version,
                search,
                isActive);

            var cachedJson = await cache.GetStringAsync(
                cacheKey, ct);

            if (cachedJson is not null)
            {
                var cachedClients =
                    JsonSerializer.Deserialize<List<ClientResponse>>(
                        cachedJson);

                if (cachedClients is not null)
                {
                    logger.LogInformation(
                        "Client list cache HIT: {CacheKey}",
                        cacheKey);

                    return Results.Ok(cachedClients);
                }
            }

            logger.LogInformation(
                "Client list cache MISS: {CacheKey}",
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
                "Redis lookup failed. Falling back to SQL Server.");

            cacheKey = null;
        }

        // Step 2: Retrieve from SQL Server.
        var query = db.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Company != null &&
                 x.Company.Contains(term)) ||
                (x.Email != null &&
                 x.Email.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == isActive.Value);
        }

        var clients = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
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
            .ToListAsync(ct);

        // Step 3: Cache results for two minutes.
        if (cacheKey is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(clients);

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
                    "Client list CACHED: {CacheKey}",
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
                    "Failed to cache Client list");
            }
        }

        return Results.Ok(clients);
    }
}
