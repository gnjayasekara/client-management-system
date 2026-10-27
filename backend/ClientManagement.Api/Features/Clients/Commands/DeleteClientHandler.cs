
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Api.Infrastructure.Realtime;
using ClientManagement.Api.Infrastructure.Messaging.Clients;
using ClientManagement.Api.Features.Clients.Events;

namespace ClientManagement.Api.Features.Clients.Commands;

public static class DeleteClientHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        IClientEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (client is null)
            return Results.NotFound();

        var clientId = client.Id;
        var clientName = client.Name;

        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);

        var logger = loggerFactory.CreateLogger("DeleteClientHandler");

        // SignalR
        try
        {
            await realtimeNotifier.ClientChangedAsync(
                clientId, "Deleted", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "SignalR failed for deleted Client {ClientId}",
                clientId);
        }

        // RabbitMQ
        try
        {
            var clientDeletedEvent = new ClientDeletedEvent(
                Guid.NewGuid(),
                clientId,
                clientName,
                DateTime.UtcNow);

            await eventPublisher.PublishDeletedAsync(
                clientDeletedEvent, ct);

            logger.LogInformation(
                "Published ClientDeleted event for Client {ClientId}",
                clientId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for deleted Client {ClientId}",
                clientId);
        }

        return Results.NoContent();
    }
}
