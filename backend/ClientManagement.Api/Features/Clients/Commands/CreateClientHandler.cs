
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Api.Infrastructure.Realtime;
using ClientManagement.Api.Features.Clients.Events;

using ClientManagement.Api.Infrastructure.Messaging.Clients;

namespace ClientManagement.Api.Features.Clients.Commands;

public static class CreateClientHandler
{
    public static async Task<IResult> HandleAsync(
        CreateClientRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        IClientEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {

        var logger = loggerFactory.CreateLogger(
            "CreateClientHandler");

        var client = new Client
        {
            Name = request.Name.Trim(),
            Company = request.Company,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = request.IsActive
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);

         // 2. Notify connected users 
        try
        {
            await realtimeNotifier.ClientChangedAsync(
                client.Id,
                "Created",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SignalR notification failed for Client {ClientId}",
                client.Id);
        }


        var clientCreatedEvent = new ClientCreatedEvent(
            Guid.NewGuid(),
            client.Id,
            client.Name,
            DateTime.UtcNow
        );

        try
        {
            await eventPublisher.PublishCreatedAsync(
                clientCreatedEvent,
                ct
            );

            logger.LogInformation(
                "Published ClientCreated event {EventId} for Client {ClientId}",
                clientCreatedEvent.EventId,
                client.Id
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for Client {ClientId}",
                client.Id
            );
        }

        
        var response = new ClientResponse(
            client.Id,
            client.Name,
            client.Company,
            client.Email,
            client.Phone,
            client.Address,
            client.IsActive,
            client.CreatedAt
        );

        return Results.Created(
            $"/api/clients/{client.Id}",
            response
        );
    }
}
