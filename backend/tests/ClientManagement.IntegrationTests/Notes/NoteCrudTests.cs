
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Features.Notes.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Notes;

public class NoteCrudTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public NoteCrudTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // Reusable setup and cleanup for each test
    private async Task RunWithTestDataAsync(
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
                        scope.ServiceProvider,
                        AppRoles.Staff);

                userId = user.Id;
                token = generatedToken;

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var client = new Client
                {
                    Name = $"Notes Test {Guid.NewGuid():N}",
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

    private static string UniqueContent() =>
        $"Integration test note {Guid.NewGuid():N}";

    private async Task<int> CreateNoteAsync(
        HttpClient http,
        int clientId,
        string content)
    {
        var response = await http.PostAsJsonAsync(
            $"/api/clients/{clientId}/notes",
            new CreateNoteRequest(content));

        Assert.True(
            response.IsSuccessStatusCode,
            $"Note creation failed: {(int)response.StatusCode}");

        // Verify persistence independently of the response DTO
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var note = await db.ClientNotes
            .AsNoTracking()
            .SingleAsync(x =>
                x.ClientId == clientId &&
                x.Content == content);

        return note.Id;
    }

    // TEST 1: Create note
    [Fact]
    public async Task Staff_ShouldCreateNote()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var content = UniqueContent();

            var noteId = await CreateNoteAsync(
                http, clientId, content);

            Assert.True(noteId > 0);
        });
    }

    // TEST 2: Retrieve notes belonging to client
    [Fact]
    public async Task GetClientNotes_ShouldReturnCreatedNote()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var content = UniqueContent();

            var noteId = await CreateNoteAsync(
                http, clientId, content);

            var response = await http.GetAsync(
                $"/api/clients/{clientId}/notes");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var notes = await response.Content
                .ReadFromJsonAsync<List<NoteResponse>>();

            Assert.NotNull(notes);

            Assert.Contains(notes, note =>
                note.Id == noteId &&
                note.ClientId == clientId &&
                note.Content == content);
        });
    }

    // TEST 3: Delete note
    [Fact]
    public async Task Staff_ShouldDeleteNote()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var noteId = await CreateNoteAsync(
                http, clientId, UniqueContent());

            var response = await http.DeleteAsync(
                $"/api/notes/{noteId}");

            Assert.True(response.IsSuccessStatusCode);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.False(await db.ClientNotes
                .AsNoTracking()
                .AnyAsync(x => x.Id == noteId));
        });
    }

    // TEST 4: Retrieve empty notes list
    [Fact]
    public async Task GetClientNotes_WhenEmpty_ShouldReturnEmptyList()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var response = await http.GetAsync(
                $"/api/clients/{clientId}/notes");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var notes = await response.Content
                .ReadFromJsonAsync<List<NoteResponse>>();

            Assert.NotNull(notes);
            Assert.Empty(notes);
        });
    }

    // TEST 5: Ensure notes are isolated by client
    [Fact]
    public async Task GetClientNotes_ShouldNotReturnOtherClientNotes()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            // Create another client
            int otherClientId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var otherClient = new Client
                {
                    Name = $"Other Client {Guid.NewGuid():N}",
                    IsActive = true
                };

                db.Clients.Add(otherClient);
                await db.SaveChangesAsync();

                otherClientId = otherClient.Id;
            }

            try
            {
                var content = UniqueContent();

                await CreateNoteAsync(
                    http, otherClientId, content);

                var response = await http.GetAsync(
                    $"/api/clients/{clientId}/notes");

                Assert.Equal(
                    HttpStatusCode.OK,
                    response.StatusCode);

                var notes = await response.Content
                    .ReadFromJsonAsync<List<NoteResponse>>();

                Assert.NotNull(notes);

                Assert.DoesNotContain(notes, note =>
                    note.ClientId == otherClientId);
            }
            finally
            {
                using var scope = _factory.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var otherClient = await db.Clients
                    .FindAsync(otherClientId);

                if (otherClient is not null)
                {
                    db.Clients.Remove(otherClient);
                    await db.SaveChangesAsync();
                }
            }
        });
    }

    // TEST 6: Unauthenticated GET
    [Fact]
    public async Task GetClientNotes_WithoutToken_ShouldReturn401()
    {
        using var http = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var response = await http.GetAsync(
            "/api/clients/1/notes");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // TEST 7: Unauthenticated POST
    [Fact]
    public async Task CreateNote_WithoutToken_ShouldReturn401()
    {
        using var http = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var response = await http.PostAsJsonAsync(
            "/api/clients/1/notes",
            new CreateNoteRequest("Unauthorized test note"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // TEST 8: Unauthenticated DELETE
    [Fact]
    public async Task DeleteNote_WithoutToken_ShouldReturn401()
    {
        using var http = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var response = await http.DeleteAsync(
            "/api/notes/1");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateNote_WithEmptyContent_ShouldReturn400(
        string content)
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var request = new CreateNoteRequest(content);

            var response = await http.PostAsJsonAsync(
                $"/api/clients/{clientId}/notes",
                request);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            // Invalid notes must not be saved.
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.False(await db.ClientNotes.AsNoTracking()
                .AnyAsync(x => x.ClientId == clientId));
        });
    }

    [Fact]
    public async Task CreateNote_WithContentOver2000_ShouldReturn400()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var request = new CreateNoteRequest(
                new string('A', 2001));

            var response = await http.PostAsJsonAsync(
                $"/api/clients/{clientId}/notes",
                request);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        });
    }

    [Fact]
    public async Task CreateNote_WithExactly2000Characters_ShouldSucceed()
    {
        await RunWithTestDataAsync(async (http, clientId) =>
        {
            var content = new string('A', 2000);

            var response = await http.PostAsJsonAsync(
                $"/api/clients/{clientId}/notes",
                new CreateNoteRequest(content));

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var saved = await db.ClientNotes.AsNoTracking()
                .SingleAsync(x =>
                    x.ClientId == clientId &&
                    x.Content == content);

            Assert.Equal(2000, saved.Content.Length);
        });
    }

    [Fact]
    public async Task CreateNote_ForMissingClient_ShouldReturn404()
    {
        await RunWithTestDataAsync(async (http, _) =>
        {
            var response = await http.PostAsJsonAsync(
                $"/api/clients/{int.MaxValue}/notes",
                new CreateNoteRequest("Test note"));

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        });
    }

    [Fact]
    public async Task GetNotes_ForMissingClient_ShouldReturn404()
    {
        await RunWithTestDataAsync(async (http, _) =>
        {
            var response = await http.GetAsync(
                $"/api/clients/{int.MaxValue}/notes");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        });
    }

    [Fact]
    public async Task DeleteNote_WhenNotFound_ShouldReturn404()
    {
        await RunWithTestDataAsync(async (http, _) =>
        {
            var response = await http.DeleteAsync(
                $"/api/notes/{int.MaxValue}");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        });
    }

}

