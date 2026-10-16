
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

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<AppUser> userManager,
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

            var (token, expiresAt) =
                tokenService.GenerateToken(user);

            return Results.Ok(new LoginResponse(
                token,
                "Bearer",
                expiresAt,
                user.UserName ?? string.Empty
            ));
        });

        return group;
    }
}
