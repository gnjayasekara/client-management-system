namespace ClientManagement.Api.Features.Auth.DTOs;

public record RegisterUserRequest(
    string Username,
    string Email,
    string Password,
    string Role
);
