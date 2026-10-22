
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Auth;

public class AuthWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    private const string InitialPassword = "TestPassword123!";
    private const string NewPassword = "NewPassword123!";

    public AuthWorkflowTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateHttpClient(string? token = null)
    {
        var http = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return http;
    }

    // Create a real test user, execute a scenario, and clean up.
    private async Task RunWithUserAsync(
        Func<AppUser, string, Task> test)
    {
        await TestDatabase.InitializeAsync(_factory);

        string? userId = null;

        try
        {
            AppUser user;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                (user, token) = await TestUserHelper.CreateAsync(
                    scope.ServiceProvider, AppRoles.Staff);

                userId = user.Id;
            }

            await test(user, token);
        }
        finally
        {
            if (userId is not null)
            {
                using var scope = _factory.Services.CreateScope();

                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var existing = await userManager.FindByIdAsync(userId);

                if (existing is not null)
                {
                    var result = await userManager.DeleteAsync(existing);
                    Assert.True(result.Succeeded);
                }
            }
        }
    }

    private async Task UpdateAccountAsync(
        string userId,
        bool? emailConfirmed = null,
        bool? mustChangePassword = null)
    {
        using var scope = _factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByIdAsync(userId);
        Assert.NotNull(user);

        if (emailConfirmed.HasValue)
            user.EmailConfirmed = emailConfirmed.Value;

        if (mustChangePassword.HasValue)
            user.MustChangePassword = mustChangePassword.Value;

        var result = await userManager.UpdateAsync(user);
        Assert.True(result.Succeeded);
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
    }

    // TEST 1
    [Fact]
    public async Task Login_WithCorrectCredentials_ShouldReturnJwt()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttpClient();

            var response = await http.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Username = user.UserName,
                    Password = InitialPassword
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var json = await ReadJsonAsync(response);

            var root = json.RootElement;

            Assert.False(string.IsNullOrWhiteSpace(
                root.GetProperty("accessToken").GetString()));

            Assert.Equal(
                "Bearer",
                root.GetProperty("tokenType").GetString());

            Assert.False(
                root.GetProperty("mustChangePassword").GetBoolean());

            Assert.False(string.IsNullOrWhiteSpace(
                root.GetProperty("refreshToken").GetString()));
        });
    }

    // TEST 2
    [Fact]
    public async Task Login_WithIncorrectPassword_ShouldReturn401()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttpClient();

            var response = await http.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Username = user.UserName,
                    Password = "WrongPassword123!"
                });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        });
    }

    // TEST 3
    [Fact]
    public async Task Login_WithUnverifiedEmail_ShouldReturn403()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            await UpdateAccountAsync(
                user.Id,
                emailConfirmed: false);

            using var http = CreateHttpClient();

            var response = await http.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Username = user.UserName,
                    Password = InitialPassword
                });

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            using var json = await ReadJsonAsync(response);

            Assert.Equal(
                "EMAIL_NOT_VERIFIED",
                json.RootElement.GetProperty("code").GetString());
        });
    }

    // TEST 4
    [Fact]
    public async Task Login_WhenPasswordChangeRequired_ShouldReturnFlag()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            await UpdateAccountAsync(
                user.Id,
                mustChangePassword: true);

            using var http = CreateHttpClient();

            var response = await http.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Username = user.UserName,
                    Password = InitialPassword
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var json = await ReadJsonAsync(response);

            Assert.True(
                json.RootElement
                    .GetProperty("mustChangePassword")
                    .GetBoolean());
        });
    }

    // TEST 5
    [Fact]
    public async Task ConfirmEmail_WithValidToken_ShouldVerifyUser()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            await UpdateAccountAsync(
                user.Id,
                emailConfirmed: false);

            string confirmationToken;

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var storedUser =
                    await userManager.FindByIdAsync(user.Id);

                Assert.NotNull(storedUser);

                confirmationToken =
                    await userManager.GenerateEmailConfirmationTokenAsync(
                        storedUser);
            }

            var encodedToken = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(confirmationToken));

            using var http = CreateHttpClient();

            var response = await http.PostAsJsonAsync(
                "/api/auth/confirm-email",
                new
                {
                    UserId = user.Id,
                    Token = encodedToken
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();

            var verifyManager = verifyScope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

            var verified = await verifyManager.FindByIdAsync(user.Id);

            Assert.NotNull(verified);
            Assert.True(verified.EmailConfirmed);
        });
    }

    // TEST 6
    [Fact]
    public async Task ChangePassword_ShouldClearMandatoryPasswordFlag()
    {
        await RunWithUserAsync(async (user, token) =>
        {
            await UpdateAccountAsync(
                user.Id,
                mustChangePassword: true);

            using var http = CreateHttpClient(token);

            var response = await http.PostAsJsonAsync(
                "/api/auth/change-password",
                new
                {
                    CurrentPassword = InitialPassword,
                    NewPassword,
                    ConfirmPassword = NewPassword
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();

            var manager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

            var updated = await manager.FindByIdAsync(user.Id);

            Assert.NotNull(updated);
            Assert.False(updated.MustChangePassword);

            Assert.True(await manager.CheckPasswordAsync(
                updated, NewPassword));

            Assert.False(await manager.CheckPasswordAsync(
                updated, InitialPassword));
        });
    }

    // TEST 7
    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturn400()
    {
        await RunWithUserAsync(async (user, token) =>
        {
            using var http = CreateHttpClient(token);

            var response = await http.PostAsJsonAsync(
                "/api/auth/change-password",
                new
                {
                    CurrentPassword = "WrongPassword123!",
                    NewPassword,
                    ConfirmPassword = NewPassword
                });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        });
    }

    // TEST 8
    [Fact]
    public async Task Refresh_WithValidToken_ShouldRotateRefreshToken()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttpClient();

            // Login to obtain a refresh token.
            var loginResponse = await http.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Username = user.UserName,
                    Password = InitialPassword
                });

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            using var loginJson = await ReadJsonAsync(loginResponse);

            var originalRefreshToken = loginJson.RootElement
                .GetProperty("refreshToken").GetString();

            Assert.False(string.IsNullOrWhiteSpace(
                originalRefreshToken));

            // Exchange it for a new token pair.
            var refreshResponse = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new
                {
                    UserId = user.Id,
                    RefreshToken = originalRefreshToken
                });

            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

            using var refreshJson = await ReadJsonAsync(refreshResponse);

            var newRefreshToken = refreshJson.RootElement
                .GetProperty("refreshToken").GetString();

            Assert.False(string.IsNullOrWhiteSpace(newRefreshToken));

            Assert.NotEqual(originalRefreshToken, newRefreshToken);

            // The old refresh token must no longer work.
            var reusedResponse = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new
                {
                    UserId = user.Id,
                    RefreshToken = originalRefreshToken
                });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                reusedResponse.StatusCode);
        });
    }
}
