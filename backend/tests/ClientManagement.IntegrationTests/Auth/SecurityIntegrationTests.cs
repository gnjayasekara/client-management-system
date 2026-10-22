
using System.Net;
using System.Net.Http.Headers;
using ClientManagement.Api.Features.Auth.Services;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Auth;

public class SecurityIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SecurityIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var http = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return http;
    }

    private async Task RunWithUserAsync(
        Func<AppUser, string, IServiceProvider, Task> test)
    {
        await TestDatabase.InitializeAsync(_factory);

        using var scope = _factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var (user, token) = await TestUserHelper.CreateAsync(
            services, AppRoles.Staff);

        try
        {
            await test(user, token, services);
        }
        finally
        {
            // Clean up the temporary Identity user
            var userManager = services
                .GetRequiredService<UserManager<AppUser>>();

            var existing = await userManager.FindByIdAsync(user.Id);

            if (existing is not null)
            {
                var result = await userManager.DeleteAsync(existing);
                Assert.True(result.Succeeded);
            }
        }
    }

    [Fact]
    public async Task VerifiedUser_ShouldAccessProtectedEndpoint()
    {
        await RunWithUserAsync(async (user, token, services) =>
        {
            using var http = CreateAuthenticatedClient(token);

            var response = await http.GetAsync("/api/clients");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task UnverifiedEmail_ShouldReturn403()
    {
        await RunWithUserAsync(async (user, token, services) =>
        {
            var userManager = services
                .GetRequiredService<UserManager<AppUser>>();

            user.EmailConfirmed = false;

            var update = await userManager.UpdateAsync(user);
            Assert.True(update.Succeeded);

            using var http = CreateAuthenticatedClient(token);

            var response = await http.GetAsync("/api/clients");

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("EMAIL_NOT_VERIFIED", body);
        });
    }

    [Fact]
    public async Task MustChangePassword_ShouldReturn403()
    {
        await RunWithUserAsync(async (user, token, services) =>
        {
            var userManager = services
                .GetRequiredService<UserManager<AppUser>>();

            user.MustChangePassword = true;

            var update = await userManager.UpdateAsync(user);
            Assert.True(update.Succeeded);

            using var http = CreateAuthenticatedClient(token);

            var response = await http.GetAsync("/api/clients");

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("PASSWORD_CHANGE_REQUIRED", body);
        });
    }

    [Fact]
    public async Task DeletedUser_WithValidToken_ShouldReturn401()
    {
        await RunWithUserAsync(async (user, token, services) =>
        {
            var userManager = services
                .GetRequiredService<UserManager<AppUser>>();

            var delete = await userManager.DeleteAsync(user);
            Assert.True(delete.Succeeded);

            using var http = CreateAuthenticatedClient(token);

            var response = await http.GetAsync("/api/clients");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        });
    }
}
