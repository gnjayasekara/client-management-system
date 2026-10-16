namespace ClientManagement.Api.Features.Auth.DTOs;

public record ConfirmEmailRequest(
    string UserId,
    string Token
);