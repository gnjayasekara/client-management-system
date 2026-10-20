
using ClientManagement.Api.Features.Auth.DTOs;
using ClientManagement.Api.Features.Auth.Services;
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

using System.Text;
using Microsoft.AspNetCore.WebUtilities;




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
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILoggerFactory loggerFactory,
            IConfiguration configuration) =>
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
                EmailConfirmed = false,
                MustChangePassword = true
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

            
            var confirmationToken =
                await userManager.GenerateEmailConfirmationTokenAsync(newUser);

            var encodedToken = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(confirmationToken)
            );

            var frontendBaseUrl =
                configuration["EmailSettings:FrontendBaseUrl"]
                ?? throw new InvalidOperationException(
                    "Frontend URL is not configured.");

            var verificationUrl =
                $"{frontendBaseUrl.TrimEnd('/')}/confirm-email" +
                $"?userId={Uri.EscapeDataString(newUser.Id)}" +
                $"&token={encodedToken}";

            
            try
            {
                await emailService.SendVerificationEmailAsync(
                    newUser.Email!,
                    verificationUrl);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger(
                    "ClientManagement.Api.Features.Auth.AuthEndpoints"
                );

                logger.LogError(
                    ex,
                    "Failed to send verification email for user {UserId}",
                    newUser.Id
                );

                return Results.Problem(
                    title: "Account created, but verification email could not be sent.",
                    statusCode: StatusCodes.Status503ServiceUnavailable
                );
            }

            return Results.Ok(new
            {
                message = "User created successfully. A verification email has been sent.",
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

            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                return Results.Json(
                    new
                    {
                        code = "EMAIL_NOT_VERIFIED",
                        message = "Please verify your email before logging in."
                    },
                    statusCode: StatusCodes.Status403Forbidden
                );
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
                refreshToken,
                user.MustChangePassword
            ));
        });

        
        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService tokenService) =>
        {
            // 1. Validate request
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new
                {
                    message = "UserId and RefreshToken are required."
                });
            }

            // 2. Find user
            var user = await userManager.FindByIdAsync(request.UserId);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            // 3. Validate stored refresh token
            var storedToken = await userManager.GetAuthenticationTokenAsync(
                user,
                "ClientManagementApi",
                "RefreshToken"
            );

            if (storedToken is null ||
                storedToken != request.RefreshToken)
            {
                return Results.Unauthorized();
            }

            // 4. Check email verification BEFORE issuing tokens
            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                return Results.Json(
                    new
                    {
                        message = "Please verify your email before continuing."
                    },
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            // 5. Require a new login if password change is pending
            if (user.MustChangePassword)
            {
                return Results.Json(
                    new
                    {
                        code = "PASSWORD_CHANGE_REQUIRED",
                        message = "Please change your password before refreshing your session."
                    },
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            // 6. Retrieve roles
            var roles = await userManager.GetRolesAsync(user);

            // 7. Retrieve permissions from role claims
            var permissions = new HashSet<string>();

            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);

                if (role is not null)
                {
                    var claims = await roleManager.GetClaimsAsync(role);

                    foreach (var claim in claims.Where(
                        c => c.Type == "permission"))
                    {
                        permissions.Add(claim.Value);
                    }
                }
            }

            // 8. Generate new JWT
            var (newToken, expiresAt) =
                tokenService.GenerateToken(user, roles, permissions);

            // 9. Rotate refresh token
            var newRefreshToken = Guid.NewGuid().ToString("N");

            var tokenResult = await userManager.SetAuthenticationTokenAsync(
                user,
                "ClientManagementApi",
                "RefreshToken",
                newRefreshToken
            );

            if (!tokenResult.Succeeded)
            {
                return Results.Problem(
                    title: "Unable to refresh authentication session.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            // 10. Return refreshed authentication details
            return Results.Ok(new LoginResponse(
                newToken,
                "Bearer",
                expiresAt,
                user.UserName ?? string.Empty,
                roles.ToList(),
                permissions.ToList(),
                newRefreshToken,
                user.MustChangePassword
            ));
        });


        // 1. Email Confirmation Token Endpoint
        
        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest request,
            UserManager<AppUser> userManager) =>
        {
            // 1. Validate request
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.Token))
            {
                return Results.BadRequest(new
                {
                    message = "UserId and Token are required."
                });
            }

            // 2. Find user
            var user = await userManager.FindByIdAsync(request.UserId);

            if (user is null)
            {
                return Results.NotFound(new
                {
                    message = "User not found."
                });
            }

            // 3. Check whether already confirmed
            if (await userManager.IsEmailConfirmedAsync(user))
            {
                return Results.Ok(new
                {
                    message = "Email is already verified."
                });
            }

            // 4. Decode the confirmation token
            string decodedToken;

            try
            {
                decodedToken = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(request.Token)
                );
            }
            catch (FormatException)
            {
                return Results.BadRequest(new
                {
                    message = "Invalid verification token format."
                });
            }

            // 5. Confirm email using ASP.NET Core Identity
            var result = await userManager.ConfirmEmailAsync(
                user,
                decodedToken
            );

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => e.Description)
                    .ToArray();

                return Results.BadRequest(new
                {
                    message = "Email confirmation failed.",
                    errors
                });
            }

            // 6. Success
            return Results.Ok(new
            {
                message = "Email confirmed successfully."
            });
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
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return Results.BadRequest(new
                {
                    message = "All fields are required."
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Results.BadRequest(new
                {
                    message = "New password and confirmation do not match."
                });
            }

            var user = await userManager.FindByIdAsync(request.UserId);

            if (user is null)
            {
                return Results.BadRequest(new
                {
                    message = "Invalid password reset request."
                });
            }

            // Reset password using ASP.NET Core Identity
            var result = await userManager.ResetPasswordAsync(
                user,
                request.Token,
                request.NewPassword
            );

            if (!result.Succeeded)
            {
                return Results.BadRequest(new
                {
                    message = "Password reset failed.",
                    errors = result.Errors
                        .Select(e => e.Description)
                        .ToArray()
                });
            }

            // Clear mandatory first-login password change
            if (user.MustChangePassword)
            {
                user.MustChangePassword = false;

                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return Results.Problem(
                        title: "Password was reset, but account setup could not be completed.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            }

            // Invalidate existing refresh token
            var removeTokenResult =
                await userManager.RemoveAuthenticationTokenAsync(
                    user,
                    "ClientManagementApi",
                    "RefreshToken"
                );

            if (!removeTokenResult.Succeeded)
            {
                return Results.Problem(
                    title: "Password reset, but the existing session could not be revoked.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            return Results.Ok(new
            {
                message = "Password reset successfully. Please log in with your new password."
            });
        });


        
        
        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            System.Security.Claims.ClaimsPrincipal caller,
            UserManager<AppUser> userManager) =>
        {
            // 1. Validate password fields
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return Results.BadRequest(new
                {
                    message = "All password fields are required."
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Results.BadRequest(new
                {
                    message = "New password and confirmation do not match."
                });
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                return Results.BadRequest(new
                {
                    message = "New password must be different from current password."
                });
            }

            // 2. Get authenticated user's ID from JWT
            var userId = caller.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            // 3. Find the authenticated user
            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            // 4. Change password using ASP.NET Core Identity
            
            var result = await userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword
            );

            if (!result.Succeeded)
            {
                return Results.BadRequest(new
                {
                    message = "Password change failed.",
                    errors = result.Errors
                        .Select(e => e.Description)
                        .ToArray()
                });
            }

            // Clear mandatory password change requirement
            user.MustChangePassword = false;

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return Results.Problem(
                    title: "Password changed, but account status could not be updated.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            // Revoke existing refresh token
            var revokeResult = await userManager.RemoveAuthenticationTokenAsync(
                user,
                "ClientManagementApi",
                "RefreshToken"
            );

            if (!revokeResult.Succeeded)
            {
                return Results.Problem(
                    title: "Password changed, but session revocation failed.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            return Results.Ok(new
            {
                message = "Password changed successfully. Please log in again."
            });

        })
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);
        ;


        return group;
    }
}
