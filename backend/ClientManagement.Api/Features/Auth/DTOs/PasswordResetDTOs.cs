namespace ClientManagement.Api.Features.Auth.DTOs;

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string UserId,
    string Token,
    string NewPassword
);
