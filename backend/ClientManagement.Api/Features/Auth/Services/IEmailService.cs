
namespace ClientManagement.Api.Features.Auth.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationUrl,
        CancellationToken ct = default);
}
