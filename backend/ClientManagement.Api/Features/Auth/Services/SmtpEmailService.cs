
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ClientManagement.Api.Features.Auth.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationUrl,
        CancellationToken ct = default)
    {
        var host = _configuration["EmailSettings:Host"]
            ?? throw new InvalidOperationException(
                "SMTP host is not configured.");

        var port = _configuration.GetValue<int>(
            "EmailSettings:Port");

        var senderEmail =
            _configuration["EmailSettings:SenderEmail"]
            ?? throw new InvalidOperationException(
                "Sender email is not configured.");

        var username =
            _configuration["EmailSettings:Username"]
            ?? throw new InvalidOperationException(
                "SMTP username is not configured.");

        var password =
            _configuration["EmailSettings:Password"]
            ?? throw new InvalidOperationException(
                "SMTP password is not configured.");

        var senderName =
            _configuration["EmailSettings:SenderName"]
            ?? "Client Management System";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(senderName, senderEmail));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject = "Verify your email address";

        var safeUrl = System.Net.WebUtility.HtmlEncode(
            verificationUrl);

        var body = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Welcome to Client Management System</h2>

                <p>Your account has been created.</p>

                <p>
                    Please verify your email address
                    before logging in.
                </p>

                <p>
                    <a href="{safeUrl}">
                        Verify Email Address
                    </a>
                </p>

                <p>
                    If you did not expect this email,
                    you can ignore it.
                </p>
                """,
            TextBody =
                "Your account has been created.\n\n" +
                "Verify your email address using this link:\n" +
                verificationUrl
        };

        message.Body = body.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            host,
            port,
            SecureSocketOptions.StartTls,
            ct);

        await smtp.AuthenticateAsync(
            username,
            password,
            ct);

        await smtp.SendAsync(message, ct);

        await smtp.DisconnectAsync(true, ct);
    }
}
