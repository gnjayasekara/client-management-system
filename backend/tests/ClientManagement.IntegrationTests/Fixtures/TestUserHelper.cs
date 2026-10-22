
using ClientManagement.Api.Features.Auth.Services;
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClientManagement.IntegrationTests.Fixtures;

public static class TestUserHelper
{
    public static async Task<(AppUser User, string Token)> CreateAsync(
        IServiceProvider services,
        string role)
    {
        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        var jwtService =
            services.GetRequiredService<JwtTokenService>();

        // Test users must be unique across test runs.
        var uniqueId = Guid.NewGuid().ToString("N");

        var user = new AppUser
        {
            UserName = $"test_{uniqueId}",
            Email = $"test_{uniqueId}@example.com",
            EmailConfirmed = true,
            MustChangePassword = false
        };

        var createResult = await userManager.CreateAsync(
            user,
            "TestPassword123!");

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    createResult.Errors.Select(e => e.Description)));
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult =
                await roleManager.CreateAsync(new IdentityRole(role));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create test role.");
            }
        }

        var addRoleResult =
            await userManager.AddToRoleAsync(user, role);

        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to assign test role.");
        }

        var token = jwtService.GenerateToken(
            user,
            roles: new[] { role },
            permissions: Array.Empty<string>()
        ).Token;

        return (user, token);
    }
}
