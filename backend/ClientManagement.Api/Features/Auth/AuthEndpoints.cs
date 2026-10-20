
using ClientManagement.Api.Features.Auth.DTOs;
using ClientManagement.Api.Features.Auth.Services;
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClientManagement.Api.Features.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterUserRequest request,
            System.Security.Claims.ClaimsPrincipal caller,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return Results.BadRequest(new { message = "Username, email, password, and role are required." });
            }

            var targetRole = request.Role.Trim();
            if (!AppRoles.All.Contains(targetRole))
            {
                return Results.BadRequest(new { message = $"Invalid role '{targetRole}'. Valid roles are: {string.Join(", ", AppRoles.All)}" });
            }

            // Hierarchy validation:
            // SuperAdmin can create Admin, Manager, Staff (or SuperAdmin).
            // Admin can create Manager (or Staff).
            // Manager can create Staff.
            // Staff cannot create any user.
            var isSuperAdmin = caller.IsInRole(AppRoles.SuperAdmin);
            var isAdmin = caller.IsInRole(AppRoles.Admin);
            var isManager = caller.IsInRole(AppRoles.Manager);

            bool isAllowed = false;
            if (isSuperAdmin)
            {
                // SuperAdmin can create any role
                isAllowed = true;
            }
            else if (isAdmin)
            {
                // Admin can create Manager or Staff (cannot create Admin or SuperAdmin)
                isAllowed = targetRole == AppRoles.Manager || targetRole == AppRoles.Staff;
            }
            else if (isManager)
            {
                // Manager can only create Staff
                isAllowed = targetRole == AppRoles.Staff;
            }

            if (!isAllowed)
            {
                return Results.Json(
                    new { message = "You do not have permission to create a user with this role hierarchy." },
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            if (!await roleManager.RoleExistsAsync(targetRole))
            {
                return Results.BadRequest(new { message = $"Role '{targetRole}' does not exist." });
            }

            var existingUser = await userManager.FindByNameAsync(request.Username);
            if (existingUser is not null)
            {
                return Results.Conflict(new { message = "A user with this username already exists." });
            }

            var existingEmail = await userManager.FindByEmailAsync(request.Email);
            if (existingEmail is not null)
            {
                return Results.Conflict(new { message = "A user with this email already exists." });
            }

            var newUser = new AppUser
            {
                UserName = request.Username.Trim(),
                Email = request.Email.Trim(),
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message = $"User creation failed: {errors}" });
            }

            var roleResult = await userManager.AddToRoleAsync(newUser, targetRole);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message = $"Assigning role failed: {errors}" });
            }

            return Results.Ok(new
            {
                message = "User registered successfully.",
                userId = newUser.Id,
                username = newUser.UserName,
                role = targetRole
            });
        }).RequireAuthorization();

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new
                {
                    message = "Username and password are required."
                });
            }

            var user = await userManager.FindByNameAsync(
                request.Username);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var validPassword = await userManager
                .CheckPasswordAsync(user, request.Password);

            if (!validPassword)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);

            var permissions = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role is not null)
                {
                    var claims = await roleManager.GetClaimsAsync(role);
                    foreach (var claim in claims.Where(c => c.Type == "permission"))
                    {
                        permissions.Add(claim.Value);
                    }
                }
            }

            var (token, expiresAt) =
                tokenService.GenerateToken(user, roles, permissions);

            var refreshToken = Guid.NewGuid().ToString("N");
            await userManager.SetAuthenticationTokenAsync(
                user,
                "ClientManagementApi",
                "RefreshToken",
                refreshToken
            );

            return Results.Ok(new LoginResponse(
                token,
                "Bearer",
                expiresAt,
                user.UserName ?? string.Empty,
                roles.ToList(),
                permissions.ToList(),
                refreshToken
            ));
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new { message = "UserId and RefreshToken are required." });
            }

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var storedToken = await userManager.GetAuthenticationTokenAsync(
                user,
                "ClientManagementApi",
                "RefreshToken"
            );

            if (storedToken is null || storedToken != request.RefreshToken)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);

            var permissions = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role is not null)
                {
                    var claims = await roleManager.GetClaimsAsync(role);
                    foreach (var claim in claims.Where(c => c.Type == "permission"))
                    {
                        permissions.Add(claim.Value);
                    }
                }
            }

            var (newToken, expiresAt) =
                tokenService.GenerateToken(user, roles, permissions);

            var newRefreshToken = Guid.NewGuid().ToString("N");
            await userManager.SetAuthenticationTokenAsync(
                user,
                "ClientManagementApi",
                "RefreshToken",
                newRefreshToken
            );

            return Results.Ok(new LoginResponse(
                newToken,
                "Bearer",
                expiresAt,
                user.UserName ?? string.Empty,
                roles.ToList(),
                permissions.ToList(),
                newRefreshToken
            ));
        });

        // 1. Email Confirmation Token Endpoint
        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest request,
            UserManager<AppUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.Token))
            {
                return Results.BadRequest(new { message = "UserId and Token are required." });
            }

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return Results.NotFound(new { message = "User not found." });
            }

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message = $"Email confirmation failed: {errors}" });
            }

            return Results.Ok(new { message = "Email confirmed successfully." });
        });

        // 2. Forgot Password (Generates standard Identity PasswordResetToken)
        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            UserManager<AppUser> userManager,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { message = "Email is required." });
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                // Return Ok to avoid leaking whether user exists
                return Results.Ok(new { message = "If the email is registered, a password reset token has been generated." });
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

            // In production, send via email provider. In development, log the token.
            logger.LogInformation("Password reset token for user {UserId}: {Token}", user.Id, resetToken);

            return Results.Ok(new
            {
                message = "If the email is registered, a password reset token has been generated.",
                userId = user.Id,
                token = resetToken // provided for development testing
            });
        });

        // 3. Reset Password (Validates standard Identity PasswordResetToken)
        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            UserManager<AppUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { message = "UserId, Token, and NewPassword are required." });
            }

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return Results.NotFound(new { message = "User not found." });
            }

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message = $"Password reset failed: {errors}" });
            }

            return Results.Ok(new { message = "Password has been successfully reset." });
        });

        return group;
    }
}
