
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ClientManagement.Api.Infrastructure.Realtime;

[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly UserManager<AppUser> _userManager;

    public NotificationHub(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

   public override async Task OnConnectedAsync()
    {
        var userId =
            Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            Context.Abort();
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null ||
            !await _userManager.IsEmailConfirmedAsync(user) ||
            user.MustChangePassword)
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }



    // public override async Task OnDisconnectedAsync(Exception? exception)
    // {
    //     var connectionId = Context.ConnectionId;
    //     var userId = Context.User?.Identity?.Name;

    //     if (userId != null)
    //     {
    //         await Groups.RemoveFromGroupAsync(connectionId, userId);
    //         __userManager.LogInformation(
    //             "User {UserId} disconnected from connection ID {ConnectionId}.",
    //             userId, connectionId);
    //     }
    //     else
    //     {
    //         _userManager.LogWarning(
    //             "A user disconnected without a valid identity. Connection ID: {ConnectionId}.",
    //             connectionId);
    //     }

    //     await base.OnDisconnectedAsync(exception);
    // }
}