
using System.Net;
using System.Net.Http.Headers;
using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Features.FollowUps;
using ClientManagement.Api.Features.Notes;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace ClientManagement.IntegrationTests.Clients;

public class CascadeDeletionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CascadeDeletionTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteClient_ShouldCascadeDeleteFollowUpsAndNotes()
    {
        await TestDatabase.InitializeAsync(_factory);

        string userId = "";
        int? clientId = null;

        try
        {
            // Arrange: Create an authenticated Admin
            using (var scope = _factory.Services.CreateScope())
            {
                var (user, token) = await TestUserHelper.CreateAsync(
                    scope.ServiceProvider,
                    AppRoles.Admin);

                userId = user.Id;

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var client = new Client
                {
                    Name = $"Cascade Test {Guid.NewGuid():N}",
                    Company = "Test Company",
                    IsActive = true
                };

                db.Clients.Add(client);
                await db.SaveChangesAsync();

                clientId = client.Id;

                var followUp = new FollowUp
                {
                    ClientId = client.Id,
                    Title = "Test follow-up",
                    DueDate = DateTime.UtcNow.AddDays(1),
                    IsCompleted = false
                };

                var note = new ClientNote
                {
                    ClientId = client.Id,
                    Content = "Test note"
                };

                db.FollowUps.Add(followUp);
                db.ClientNotes.Add(note);

                await db.SaveChangesAsync();

                using var http = _factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        BaseAddress = new Uri("https://localhost")
                    });

                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Act: Delete parent through the real API
                var response = await http.DeleteAsync(
                    $"/api/clients/{clientId}");

                Assert.Equal(
                    HttpStatusCode.NoContent,
                    response.StatusCode);
            }

            // Assert: Check database using a fresh DbContext
            using var verifyScope = _factory.Services.CreateScope();

            var verifyDb = verifyScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            Assert.False(await verifyDb.Clients
                .AsNoTracking()
                .AnyAsync(x => x.Id == clientId));

            Assert.False(await verifyDb.FollowUps
                .AsNoTracking()
                .AnyAsync(x => x.ClientId == clientId));

            Assert.False(await verifyDb.ClientNotes
                .AsNoTracking()
                .AnyAsync(x => x.ClientId == clientId));
        }
        finally
        {
            // Clean up even if an assertion fails.
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            if (clientId.HasValue)
            {
                var remainingClient =
                    await db.Clients.FindAsync(clientId.Value);

                if (remainingClient is not null)
                {
                    db.Clients.Remove(remainingClient);
                    await db.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var user = await userManager.FindByIdAsync(userId);

                if (user is not null)
                {
                    await userManager.DeleteAsync(user);
                }
            }
        }
    }
}
