
namespace ClientManagement.Api.Features.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    string Username
);
