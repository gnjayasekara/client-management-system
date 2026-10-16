
namespace ClientManagement.Api.Features.Auth.DTOs;

public record LoginRequest(
    string Username,
    string Password
);
