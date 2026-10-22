
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Database;

public class DatabaseIntegrationTests
    : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DatabaseIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_ShouldConnectSuccessfully()
    {
        // Arrange
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Act
        var canConnect = await db.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Database_ShouldHaveClientsTable()
    {
        // Arrange
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Act
        var clients = await db.Clients
            .AsNoTracking()
            .ToListAsync();

        // Assert
        Assert.NotNull(clients);
    }

    [Fact]
    public async Task Database_ShouldHaveNoPendingMigrations()
    {
        // Arrange
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Act
        var pendingMigrations =
            await db.Database.GetPendingMigrationsAsync();

        // Assert
        Assert.Empty(pendingMigrations);
    }
}
