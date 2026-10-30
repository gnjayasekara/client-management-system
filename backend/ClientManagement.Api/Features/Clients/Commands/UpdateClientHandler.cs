
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Api.Infrastructure.Realtime;

using ClientManagement.Api.Features.Clients.Events;
using ClientManagement.Api.Infrastructure.Messaging.Clients;

namespace ClientManagement.Api.Features.Clients.Commands;

public static class UpdateClientHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        UpdateClientRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        IClientEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
                    "UpdateClientHandler");

        var client = await db.Clients
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (client is null)
            return Results.NotFound();

        client.Name = request.Name.Trim();
        client.Company = request.Company;
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.Address = request.Address;
        client.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        try
        {
            await realtimeNotifier.ClientChangedAsync(
                client.Id, "Updated", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "SignalR failed for Client {ClientId}", client.Id);
        }

        // RabbitMQ event publishing
        try
        {
            var clientUpdatedEvent = new ClientUpdatedEvent(
                Guid.NewGuid(),
                client.Id,
                client.Name,
                DateTime.UtcNow);

            await eventPublisher.PublishUpdatedAsync(
                clientUpdatedEvent, ct);

            logger.LogInformation(
                "Published ClientUpdated event for Client {ClientId}",
                client.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for Client {ClientId}",
                client.Id);
        }

        return Results.NoContent();
    }
}
