
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;

namespace ClientManagement.Api.Infrastructure.Caching;

public sealed class DashboardCacheInvalidationInterceptor
    : SaveChangesInterceptor
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DashboardCacheInvalidationInterceptor> _logger;

    private bool _invalidateDashboard;
    private bool _invalidateClients;

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
        var context = eventData.Context;

        context?.ChangeTracker.DetectChanges();

        var changedEntries = context?.ChangeTracker.Entries()
            .Where(entry =>
                entry.State == EntityState.Added ||
                entry.State == EntityState.Modified ||
                entry.State == EntityState.Deleted)
            .ToList();

        _invalidateDashboard = changedEntries?.Any(entry =>
            entry.Metadata.ClrType.Name == "Client" ||
            entry.Metadata.ClrType.Name == "FollowUp") == true;

        _invalidateClients = changedEntries?.Any(entry =>
            entry.Metadata.ClrType.Name == "Client") == true;

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var invalidateDashboard = _invalidateDashboard;
        var invalidateClients = _invalidateClients;

        _invalidateDashboard = false;
        _invalidateClients = false;

        if (invalidateDashboard)
        {
            try
            {
                await _cache.RemoveAsync(
                    DashboardCacheKeys.Stats,
                    cancellationToken);

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
        }

        if (invalidateClients)
        {
            try
            {
                var newVersion = Guid.NewGuid().ToString("N");

                await _cache.SetStringAsync(
                    ClientCacheKeys.Version,
                    newVersion,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromDays(1)
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Client cache INVALIDATED. New version: {Version}",
                    newVersion);
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
                    "Client cache invalidation failed");
            }
        }

        return result;
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _invalidateDashboard = false;
        _invalidateClients = false;

        return Task.CompletedTask;
    }
}
