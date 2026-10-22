
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClientManagement.Api.Infrastructure.Identity;
using ClientManagement.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClientManagement.IntegrationTests.Auth;

public class AdditionalAuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    private const string OldPassword = "TestPassword123!";
    private const string NewPassword = "NewPassword123!";

    public AdditionalAuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateHttp(string? token = null)
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
                    scope.ServiceProvider,
                    AppRoles.Staff);

                userId = user.Id;
            }

            await test(user, token);
        }
        finally
        {
            if (userId is not null)
            {
                using var scope = _factory.Services.CreateScope();

                var manager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var existing = await manager.FindByIdAsync(userId);

                if (existing is not null)
                {
                    var result = await manager.DeleteAsync(existing);
                    Assert.True(result.Succeeded);
                }
            }
        }
    }

    private async Task SetAccountStatusAsync(
        string userId,
        bool? emailConfirmed = null,
        bool? mustChangePassword = null)
    {
        using var scope = _factory.Services.CreateScope();

        var manager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var user = await manager.FindByIdAsync(userId);
        Assert.NotNull(user);

        if (emailConfirmed.HasValue)
            user.EmailConfirmed = emailConfirmed.Value;

        if (mustChangePassword.HasValue)
            user.MustChangePassword = mustChangePassword.Value;

        Assert.True((await manager.UpdateAsync(user)).Succeeded);
    }

    private static async Task<string> LoginForRefreshTokenAsync(
        HttpClient http,
        string username)
    {
        var response = await http.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = username, Password = OldPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement
            .GetProperty("refreshToken")
            .GetString()!;
    }

    // TEST 1 — Invalid refresh token
    [Fact]
    public async Task Refresh_InvalidToken_ShouldReturn401()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttp();

            var response = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new
                {
                    UserId = user.Id,
                    RefreshToken = "invalid-refresh-token"
                });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        });
    }

    // TEST 2 — Refresh blocked for unverified email
    [Fact]
    public async Task Refresh_UnverifiedEmail_ShouldReturn403()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttp();

            var refreshToken = await LoginForRefreshTokenAsync(
                http, user.UserName!);

            await SetAccountStatusAsync(
                user.Id, emailConfirmed: false);

            var response = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new
                {
                    UserId = user.Id,
                    RefreshToken = refreshToken
                });

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        });
    }

    // TEST 3 — Refresh blocked when password change pending
    [Fact]
    public async Task Refresh_PasswordChangeRequired_ShouldReturn403()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttp();

            var refreshToken = await LoginForRefreshTokenAsync(
                http, user.UserName!);

            await SetAccountStatusAsync(
                user.Id, mustChangePassword: true);

            var response = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new
                {
                    UserId = user.Id,
                    RefreshToken = refreshToken
                });

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("PASSWORD_CHANGE_REQUIRED", body);
        });
    }

    // TEST 4 — Incorrect password reset token
    [Fact]
    public async Task ResetPassword_InvalidToken_ShouldReturn400()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttp();

            var response = await http.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    UserId = user.Id,
                    Token = "invalid-reset-token",
                    NewPassword,
                    ConfirmPassword = NewPassword
                });

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        });
    }

    // TEST 5 — Successful password reset
    [Fact]
    public async Task ResetPassword_ValidToken_ShouldUpdatePassword()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            await SetAccountStatusAsync(
                user.Id, mustChangePassword: true);

            string resetToken;

            using (var scope = _factory.Services.CreateScope())
            {
                var manager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var existing = await manager.FindByIdAsync(user.Id);
                Assert.NotNull(existing);

                resetToken = await manager
                    .GeneratePasswordResetTokenAsync(existing);
            }

            using var http = CreateHttp();

            var response = await http.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    UserId = user.Id,
                    Token = resetToken,
                    NewPassword,
                    ConfirmPassword = NewPassword
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();

            var verifyManager = verifyScope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

            var updated = await verifyManager.FindByIdAsync(user.Id);
            Assert.NotNull(updated);

            Assert.False(updated.MustChangePassword);

            Assert.True(await verifyManager.CheckPasswordAsync(
                updated, NewPassword));

            Assert.False(await verifyManager.CheckPasswordAsync(
                updated, OldPassword));
        });
    }

    // TEST 6 — Password reset revokes refresh token
    [Fact]
    public async Task ResetPassword_ShouldRevokeExistingRefreshToken()
    {
        await RunWithUserAsync(async (user, _) =>
        {
            using var http = CreateHttp();

            var refreshToken = await LoginForRefreshTokenAsync(
                http, user.UserName!);

            string resetToken;

            using (var scope = _factory.Services.CreateScope())
            {
                var manager = scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

                var existing = await manager.FindByIdAsync(user.Id);
                Assert.NotNull(existing);

                resetToken = await manager
                    .GeneratePasswordResetTokenAsync(existing);
            }

            var resetResponse = await http.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    UserId = user.Id,
                    Token = resetToken,
                    NewPassword,
                    ConfirmPassword = NewPassword
                });

            Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

            var refreshResponse = await http.PostAsJsonAsync(
                "/api/auth/refresh",
                new
                {
                    UserId = user.Id,
                    RefreshToken = refreshToken
                });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                refreshResponse.StatusCode);
        });
    }
}
