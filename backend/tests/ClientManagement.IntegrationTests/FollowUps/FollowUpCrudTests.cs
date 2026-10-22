
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Features.FollowUps;
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.FollowUps;

public class FollowUpCrudTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public FollowUpCrudTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // Creates a real Identity user and parent Client,
    // executes the test, then removes the test data.
    private async Task RunWithTestDataAsync(
        string role,
        Func<HttpClient, int, Task> test)
    {
        await TestDatabase.InitializeAsync(_factory);

        string? userId = null;
        int? clientId = null;

        try
        {
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var (user, generatedToken) =
                    await TestUserHelper.CreateAsync(
                        scope.ServiceProvider, role);

                userId = user.Id;
                token = generatedToken;

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var client = new Client
                {
                    Name = $"FollowUp Test {Guid.NewGuid():N}",
                    Company = "Test Company",
                    IsActive = true
                };

                db.Clients.Add(client);
                await db.SaveChangesAsync();

                clientId = client.Id;
            }

            using var http = _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            await test(http, clientId.Value);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            // Cascade deletion removes associated Follow-ups.
            if (clientId.HasValue)
            {
                var client = await db.Clients
                    .FindAsync(clientId.Value);

                if (client is not null)
                {
                    db.Clients.Remove(client);
                    await db.SaveChangesAsync();
                }
            }

            if (userId is not null)
            {
                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var user = await userManager.FindByIdAsync(userId);

                if (user is not null)
                {
                    var result = await userManager.DeleteAsync(user);
                    Assert.True(result.Succeeded);
                }
            }
        }
    }

    private static CreateFollowUpRequest ValidRequest(
        int clientId) => new(
            ClientId: clientId,
            Title: "Call client regarding project",
            DueDate: new DateTime(2027, 1, 15)
        );

    // Read the ID without assuming the exact FollowUpResponse shape.
    private static async Task<int> ReadIdAsync(
        HttpResponseMessage response)
    {
        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<int> CreateFollowUpAsync(
        HttpClient http,
        int clientId)
    {
        var response = await http.PostAsJsonAsync(
            "/api/followups/",
            ValidRequest(clientId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadIdAsync(response);
    }

    [Fact]
    public async Task Staff_ShouldCreateFollowUp()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, clientId) =>
            {
                var id = await CreateFollowUpAsync(
                    http, clientId);

                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var saved = await db.FollowUps
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == id);

                Assert.Equal(clientId, saved.ClientId);
                Assert.Equal(
                    "Call client regarding project",
                    saved.Title);
                Assert.False(saved.IsCompleted);
            });
    }

    [Fact]
    public async Task CreateFollowUp_WithEmptyTitle_ShouldReturn400()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, clientId) =>
            {
                var request = ValidRequest(clientId) with
                {
                    Title = ""
                };

                var response = await http.PostAsJsonAsync(
                    "/api/followups/", request);

                Assert.Equal(
                    HttpStatusCode.BadRequest,
                    response.StatusCode);
            });
    }

    [Fact]
    public async Task CreateFollowUp_WithMissingClient_ShouldReturn404()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, _) =>
            {
                var request = ValidRequest(int.MaxValue);

                var response = await http.PostAsJsonAsync(
                    "/api/followups/", request);

                Assert.Equal(
                    HttpStatusCode.NotFound,
                    response.StatusCode);
            });
    }

    [Fact]
    public async Task Staff_ShouldUpdateFollowUp()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, clientId) =>
            {
                var id = await CreateFollowUpAsync(
                    http, clientId);

                var request = new UpdateFollowUpRequest(
                    ClientId: clientId,
                    Title: "Updated follow-up title",
                    DueDate: new DateTime(2027, 2, 10),
                    IsCompleted: false
                );

                var response = await http.PutAsJsonAsync(
                    $"/api/followups/{id}",
                    request);

                Assert.Equal(
                    HttpStatusCode.NoContent,
                    response.StatusCode);

                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var updated = await db.FollowUps
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == id);

                Assert.Equal(
                    "Updated follow-up title",
                    updated.Title);
                Assert.Equal(
                    new DateTime(2027, 2, 10),
                    updated.DueDate);
            });
    }

    [Fact]
    public async Task Staff_ShouldCompleteFollowUp()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, clientId) =>
            {
                var id = await CreateFollowUpAsync(
                    http, clientId);

                var response = await http.PatchAsync(
                    $"/api/followups/{id}/complete",
                    content: null);

                Assert.Equal(
                    HttpStatusCode.NoContent,
                    response.StatusCode);

                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var completed = await db.FollowUps
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == id);

                Assert.True(completed.IsCompleted);
            });
    }

    [Fact]
    public async Task Staff_ShouldNotDeleteFollowUp()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, clientId) =>
            {
                var id = await CreateFollowUpAsync(
                    http, clientId);

                var response = await http.DeleteAsync(
                    $"/api/followups/{id}");

                Assert.Equal(
                    HttpStatusCode.Forbidden,
                    response.StatusCode);

                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                Assert.True(await db.FollowUps
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == id));
            });
    }

    [Fact]
    public async Task Manager_ShouldDeleteFollowUp()
    {
        await RunWithTestDataAsync(AppRoles.Manager,
            async (http, clientId) =>
            {
                var id = await CreateFollowUpAsync(
                    http, clientId);

                var response = await http.DeleteAsync(
                    $"/api/followups/{id}");

                Assert.Equal(
                    HttpStatusCode.NoContent,
                    response.StatusCode);

                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                Assert.False(await db.FollowUps
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == id));
            });
    }

    [Fact]
    public async Task CompleteFollowUp_WhenMissing_ShouldReturn404()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, _) =>
            {
                var response = await http.PatchAsync(
                    $"/api/followups/{int.MaxValue}/complete",
                    content: null);

                Assert.Equal(
                    HttpStatusCode.NotFound,
                    response.StatusCode);
            });
    }

    [Fact]
    public async Task DeleteFollowUp_WhenMissing_ShouldReturn404()
    {
        await RunWithTestDataAsync(AppRoles.Manager,
            async (http, _) =>
            {
                var response = await http.DeleteAsync(
                    $"/api/followups/{int.MaxValue}");

                Assert.Equal(
                    HttpStatusCode.NotFound,
                    response.StatusCode);
            });
    }

    [Fact]
    public async Task UpdateFollowUp_WithMissingClient_ShouldReturn404()
    {
        await RunWithTestDataAsync(AppRoles.Staff,
            async (http, clientId) =>
            {
                var id = await CreateFollowUpAsync(
                    http, clientId);

                var request = new UpdateFollowUpRequest(
                    ClientId: int.MaxValue,
                    Title: "Updated follow-up",
                    DueDate: new DateTime(2027, 2, 10),
                    IsCompleted: false
                );

                var response = await http.PutAsJsonAsync(
                    $"/api/followups/{id}",
                    request);

                Assert.Equal(
                    HttpStatusCode.NotFound,
                    response.StatusCode);
            });
    }
}
