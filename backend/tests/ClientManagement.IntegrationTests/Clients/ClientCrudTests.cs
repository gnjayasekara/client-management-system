
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Clients;

public class ClientCrudTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ClientCrudTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string token)
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static CreateClientRequest ValidRequest() => new(
        Name: $"Test Client {Guid.NewGuid():N}",
        Company: "Test Company",
        Email: "test@example.com",
        Phone: "0771234567",
        Address: "Colombo",
        IsActive: true
    );

    private async Task CleanupAsync(string userId, int? clientId)
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        if (clientId.HasValue)
        {
            var client = await db.Clients.FindAsync(clientId.Value);

            if (client is not null)
            {
                db.Clients.Remove(client);
                await db.SaveChangesAsync();
            }
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is not null)
        {
            var result = await userManager.DeleteAsync(user);
            Assert.True(result.Succeeded);
        }
    }

    [Fact]
    public async Task Staff_ShouldCreateAndRetrieveClient()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        int? createdClientId = null;

        try
        {
            using var http = CreateClient(token);

            // POST: Create client
            var createResponse = await http.PostAsJsonAsync(
                "/api/clients/", ValidRequest());

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var created = await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

            Assert.NotNull(created);

            createdClientId = created.Id;

            // GET: Retrieve the newly created client
            var getResponse = await http.GetAsync(
                $"/api/clients/{createdClientId}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var retrieved = await getResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

            Assert.NotNull(retrieved);
            Assert.Equal(created.Id, retrieved.Id);
            Assert.Equal(created.Name, retrieved.Name);

            // Verify the record exists in SQL Server
            using var verifyScope = _factory.Services.CreateScope();

            var db = verifyScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.True(await db.Clients.AsNoTracking()
                .AnyAsync(x => x.Id == createdClientId));
        }
        finally
        {
            await CleanupAsync(user.Id, createdClientId);
        }
    }

    [Fact]
    public async Task Staff_ShouldNotDeleteClient()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        int? createdClientId = null;

        try
        {
            using var http = CreateClient(token);

            var createResponse = await http.PostAsJsonAsync(
                "/api/clients/", ValidRequest());

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var created = await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

            Assert.NotNull(created);

            createdClientId = created.Id;

            // Staff attempts DELETE
            var deleteResponse = await http.DeleteAsync(
                $"/api/clients/{createdClientId}");

            Assert.Equal(
                HttpStatusCode.Forbidden,
                deleteResponse.StatusCode);

            // Confirm the denied request did not remove the client
            using var verifyScope = _factory.Services.CreateScope();

            var db = verifyScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.True(await db.Clients.AsNoTracking()
                .AnyAsync(x => x.Id == createdClientId));
        }
        finally
        {
            await CleanupAsync(user.Id, createdClientId);
        }
    }

    [Fact]
    public async Task Admin_ShouldDeleteClient()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Admin);

        int? createdClientId = null;

        try
        {
            using var http = CreateClient(token);

            var createResponse = await http.PostAsJsonAsync(
                "/api/clients/", ValidRequest());

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var created = await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

            Assert.NotNull(created);

            createdClientId = created.Id;

            // Admin performs DELETE
            var deleteResponse = await http.DeleteAsync(
                $"/api/clients/{createdClientId}");

            Assert.True(deleteResponse.IsSuccessStatusCode);

            // Verify actual database deletion
            using var verifyScope = _factory.Services.CreateScope();

            var db = verifyScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.False(await db.Clients.AsNoTracking()
                .AnyAsync(x => x.Id == createdClientId));
        }
        finally
        {
            await CleanupAsync(user.Id, createdClientId);
        }
    }

    [Fact]
    public async Task CreateClient_WithEmptyName_ShouldReturn400()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        try
        {
            using var http = CreateClient(token);

            var request = ValidRequest() with { Name = "" };

            var response = await http.PostAsJsonAsync(
                "/api/clients/", request);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(user.Id, null);
        }
    }

    
    [Fact]
    public async Task UpdateClient_ShouldPersistChanges()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        int? clientId = null;

        try
        {
            using var http = CreateClient(token);

            var createResponse = await http.PostAsJsonAsync(
                "/api/clients/", ValidRequest());

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var created = await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

            Assert.NotNull(created);

            clientId = created.Id;

            // Arrange: Prepare new client details
            var updateRequest = new UpdateClientRequest(
                Name: "Updated Client Name",
                Company: "Updated Company",
                Email: "updated@example.com",
                Phone: "0712345678",
                Address: "Kandy",
                IsActive: false
            );

            // Act: Update through the real API
            var response = await http.PutAsJsonAsync(
                $"/api/clients/{clientId}",
                updateRequest);

            // Assert: UpdateClientHandler returns 204
            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            // Verify using a fresh EF Core context
            using var verifyScope = _factory.Services.CreateScope();

            var db = verifyScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var updated = await db.Clients
                .AsNoTracking()
                .SingleAsync(x => x.Id == clientId);

            Assert.Equal("Updated Client Name", updated.Name);
            Assert.Equal("Updated Company", updated.Company);
            Assert.Equal("updated@example.com", updated.Email);
            Assert.Equal("0712345678", updated.Phone);
            Assert.Equal("Kandy", updated.Address);
            Assert.False(updated.IsActive);
        }
        finally
        {
            await CleanupAsync(user.Id, clientId);
        }
    }

    [Fact]
    public async Task UpdateClient_WithEmptyName_ShouldReturn400()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        int? clientId = null;

        try
        {
            using var http = CreateClient(token);

            var createResponse = await http.PostAsJsonAsync(
                "/api/clients/", ValidRequest());

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var created = await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

            Assert.NotNull(created);

            clientId = created.Id;

            var updateRequest = new UpdateClientRequest(
                Name: "",
                Company: "Test Company",
                Email: "test@example.com",
                Phone: "0771234567",
                Address: "Colombo",
                IsActive: true
            );

            var response = await http.PutAsJsonAsync(
                $"/api/clients/{clientId}",
                updateRequest);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            // Invalid update must not alter the existing client
            using var verifyScope = _factory.Services.CreateScope();

            var db = verifyScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var existing = await db.Clients
                .AsNoTracking()
                .SingleAsync(x => x.Id == clientId);

            Assert.False(string.IsNullOrWhiteSpace(existing.Name));
        }
        finally
        {
            await CleanupAsync(user.Id, clientId);
        }
    }

    [Fact]
    public async Task GetClient_WhenNotFound_ShouldReturn404()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        try
        {
            using var http = CreateClient(token);

            var response = await http.GetAsync(
                "/api/clients/2147483647");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(user.Id, null);
        }
    }

    [Fact]
    public async Task UpdateClient_WhenNotFound_ShouldReturn404()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Staff);

        try
        {
            using var http = CreateClient(token);

            var updateRequest = new UpdateClientRequest(
                Name: "Missing Client",
                Company: "Test Company",
                Email: "missing@example.com",
                Phone: "0771234567",
                Address: "Colombo",
                IsActive: true
            );

            var response = await http.PutAsJsonAsync(
                "/api/clients/2147483647",
                updateRequest);

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(user.Id, null);
        }
    }

    [Fact]
    public async Task DeleteClient_WhenNotFound_ShouldReturn404()
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var (user, token) = await TestUserHelper.CreateAsync(
            scope.ServiceProvider, AppRoles.Admin);

        try
        {
            using var http = CreateClient(token);

            var response = await http.DeleteAsync(
                "/api/clients/2147483647");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(user.Id, null);
        }
    }

}
