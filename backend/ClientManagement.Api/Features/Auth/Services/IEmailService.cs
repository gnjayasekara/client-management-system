
namespace ClientManagement.Api.Features.Auth.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationUrl,
        CancellationToken ct = default);

    Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetUrl,
        CancellationToken ct = default);
}
