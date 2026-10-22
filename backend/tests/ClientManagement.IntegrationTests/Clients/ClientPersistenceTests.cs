
using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Clients;

public class ClientPersistenceTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ClientPersistenceTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateClient_ShouldPersistInDatabase()
    {
        // Arrange
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Roll back all changes made by this test.
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var client = new Client
        {
            Name = $"Integration Test {Guid.NewGuid():N}",
            Company = "Test Company",
            Email = "test@example.com",
            Phone = "0771234567",
            Address = "Colombo",
            IsActive = true
        };

        // Act
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        // Query SQL Server without using tracked objects.
        var savedClient = await db.Clients
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == client.Id);

        // Assert
        Assert.NotNull(savedClient);
        Assert.Equal(client.Name, savedClient.Name);
        Assert.Equal("Test Company", savedClient.Company);
        Assert.True(savedClient.IsActive);

        // Transaction is disposed without committing.
        // All test changes are rolled back.
    }
}
