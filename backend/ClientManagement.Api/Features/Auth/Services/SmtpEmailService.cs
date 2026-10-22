
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;

namespace ClientManagement.Api.Features.Auth.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendVerificationEmailAsync(
        string recipientEmail,
        string verificationUrl,
        CancellationToken ct = default)
    {
        var safeUrl = WebUtility.HtmlEncode(verificationUrl);

        var htmlBody = $"""
            <h2>Welcome to Client Management System</h2>

            <p>Your account has been created.</p>

            <p>Please verify your email address before logging in.</p>

            <p>
                <a href="{safeUrl}">
                    Verify Email Address
                </a>
            </p>

            <p>If you did not expect this email, you can ignore it.</p>
            """;

        var textBody =
            "Your account has been created.\n\n" +
            "Verify your email address using this link:\n" +
            verificationUrl;

        return SendEmailAsync(
            recipientEmail,
            "Verify your email address",
            htmlBody,
            textBody,
            ct);
    }

    public Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetUrl,
        CancellationToken ct = default)
    {
        var safeUrl = WebUtility.HtmlEncode(resetUrl);

        var htmlBody = $"""
            <h2>Reset Your Password</h2>

            <p>
                We received a request to reset the password
                for your Client Management System account.
            </p>

            <p>Click the button below to set a new password.</p>

            <p>
                <a href="{safeUrl}">
                    Reset Password
                </a>
            </p>

            <p>
                If you did not request a password reset,
                you can safely ignore this email.
            </p>

            <p>
                For security, do not share this link with anyone.
            </p>
            """;

        var textBody =
            "We received a request to reset your password.\n\n" +
            "Use the following link:\n" +
            resetUrl +
            "\n\nIf you did not request this, ignore this email.";

        return SendEmailAsync(
            recipientEmail,
            "Reset your Client Management System password",
            htmlBody,
            textBody,
            ct);
    }

    private async Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken ct)
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

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        }.ToMessageBody();

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
