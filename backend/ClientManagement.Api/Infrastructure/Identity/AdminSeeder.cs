
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

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        foreach (var roleName in AppRoles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new IdentityRole(roleName);
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Role creation for '{roleName}' failed: {errors}");
                }
            }

            if (AppRoles.RolePermissions.TryGetValue(roleName, out var permissions))
            {
                var existingClaims = await roleManager.GetClaimsAsync(role);
                var existingClaimValues = existingClaims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value)
                    .ToHashSet();

                foreach (var permission in permissions)
                {
                    if (!existingClaimValues.Contains(permission))
                    {
                        await roleManager.AddClaimAsync(
                            role,
                            new System.Security.Claims.Claim("permission", permission)
                        );
                    }
                }
            }
        }

        var existingUser = await userManager
            .FindByNameAsync(username);

        if (existingUser is not null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, AppRoles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(existingUser, AppRoles.SuperAdmin);
            }
            return;
        }

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

        await userManager.AddToRoleAsync(admin, AppRoles.SuperAdmin);
    }
}
