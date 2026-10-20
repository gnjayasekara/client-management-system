
namespace ClientManagement.Api.Features.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    string Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string RefreshToken,
    bool MustChangePassword
);

