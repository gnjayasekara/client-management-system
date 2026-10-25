
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;

namespace ClientManagement.Api.Infrastructure.Caching;

public sealed class DashboardCacheInvalidationInterceptor
    : SaveChangesInterceptor
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DashboardCacheInvalidationInterceptor> _logger;

    private bool _shouldInvalidate;

    public DashboardCacheInvalidationInterceptor(
        IDistributedCache cache,
        ILogger<DashboardCacheInvalidationInterceptor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        
        eventData.Context?.ChangeTracker.DetectChanges();

        _shouldInvalidate =
            eventData.Context?.ChangeTracker.Entries()
                .Any(entry =>
                    (entry.Metadata.ClrType.Name == "Client" ||
                     entry.Metadata.ClrType.Name == "FollowUp") &&
                    (entry.State == EntityState.Added ||
                     entry.State == EntityState.Modified ||
                     entry.State == EntityState.Deleted))
            == true;

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var invalidate = _shouldInvalidate;
        _shouldInvalidate = false;

        if (!invalidate)
            return result;

        try
        {
            await _cache.RemoveAsync(
                DashboardCacheKeys.Stats,
                cancellationToken
            );

            _logger.LogInformation(
                "Dashboard cache INVALIDATED after database changes");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Dashboard cache invalidation failed");
        }

        return result;
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _shouldInvalidate = false;

        return Task.CompletedTask;
    }
}
