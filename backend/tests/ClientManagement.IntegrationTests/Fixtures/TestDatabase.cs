
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClientManagement.IntegrationTests.Fixtures;

public static class TestDatabase
{
    public static async Task InitializeAsync(
        ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Safety check: never migrate another database.
        var databaseName = db.Database.GetDbConnection().Database;

        if (!string.Equals(
            databaseName,
            "ClientManagementDb_Test",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Tests must use ClientManagementDb_Test.");
        }

        // Apply existing EF Core migrations.
        await db.Database.MigrateAsync();
    }
}
