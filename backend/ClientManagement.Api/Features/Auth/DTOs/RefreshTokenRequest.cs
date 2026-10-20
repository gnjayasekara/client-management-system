namespace ClientManagement.Api.Features.Auth.DTOs;

public record RefreshTokenRequest(
    string UserId,
    string RefreshToken
);
