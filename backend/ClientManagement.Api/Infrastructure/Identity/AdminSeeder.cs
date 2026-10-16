
using Microsoft.AspNetCore.Identity;

namespace ClientManagement.Api.Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var username = configuration["SeedAdmin:Username"];
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Development admin credentials are missing.");
        }

        using var scope = services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var existingUser = await userManager
            .FindByNameAsync(username);

        if (existingUser is not null)
            return;

        var admin = new AppUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(
            admin, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Admin creation failed: {errors}");
        }
    }
}
