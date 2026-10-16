
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClientManagement.Api.Features.Auth.Services;

public class EmailService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        UserManager<AppUser> userManager,
        ILogger<EmailService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> GenerateConfirmationLinkAsync(
        AppUser user,
        string baseUrl)
    {
        // Generate Identity email confirmation token
        var token = await _userManager
            .GenerateEmailConfirmationTokenAsync(user);

        // Build confirmation URL
        var confirmationUrl =
            $"{baseUrl.TrimEnd('/')}/api/auth/confirm-email" +
            $"?userId={Uri.EscapeDataString(user.Id)}" +
            $"&token={Uri.EscapeDataString(token)}";

        // Mock email sending (development only)
        _logger.LogInformation(
            "Mock email verification requested for user {UserId}",
            user.Id);

        return confirmationUrl;
    }
}
