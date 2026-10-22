
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Auth;

public class RegistrationAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RegistrationAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Staff", "Staff", 403)]
    [InlineData("Manager", "Admin", 403)]
    [InlineData("Admin", "SuperAdmin", 403)]
    [InlineData("Manager", "Staff", 200)]
    [InlineData("Admin", "Manager", 200)]
    [InlineData("SuperAdmin", "Admin", 200)]
    public async Task Register_ShouldRespectRoleHierarchy(
        string actorRole,
        string targetRole,
        int expectedStatus)
    {
        await TestDatabase.InitializeAsync(_factory);

        string? actorUserId = null;

        var unique = Guid.NewGuid().ToString("N");
        var newUsername = $"registered_{unique}";
        var newEmail = $"{newUsername}@example.com";

        try
        {
            string actorToken;

            using (var scope = _factory.Services.CreateScope())
            {
                var roles = scope.ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole>>();

                // Registration requires target role to already exist.
                if (!await roles.RoleExistsAsync(targetRole))
                {
                    var roleResult = await roles.CreateAsync(
                        new IdentityRole(targetRole));

                    Assert.True(roleResult.Succeeded);
                }

                var (actor, token) = await TestUserHelper.CreateAsync(
                    scope.ServiceProvider,
                    actorRole);

                actorUserId = actor.Id;
                actorToken = token;
            }

            using var http = _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost")
                });

            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", actorToken);

            var response = await http.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    Username = newUsername,
                    Email = newEmail,
                    Password = "TestPassword123!",
                    Role = targetRole
                });

            Assert.Equal(expectedStatus, (int)response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();

            var manager = verifyScope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

            var registered =
                await manager.FindByNameAsync(newUsername);

            if (expectedStatus == 200)
            {
                Assert.NotNull(registered);
                Assert.False(registered.EmailConfirmed);
                Assert.True(registered.MustChangePassword);

                Assert.True(await manager.IsInRoleAsync(
                    registered, targetRole));
            }
            else
            {
                // Forbidden registration must not create an account.
                Assert.Null(registered);
            }
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();

            var manager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

            var registered =
                await manager.FindByNameAsync(newUsername);

            if (registered is not null)
            {
                var result = await manager.DeleteAsync(registered);
                Assert.True(result.Succeeded);
            }

            if (actorUserId is not null)
            {
                var actor = await manager.FindByIdAsync(actorUserId);

                if (actor is not null)
                {
                    var result = await manager.DeleteAsync(actor);
                    Assert.True(result.Succeeded);
                }
            }
        }
    }
}
