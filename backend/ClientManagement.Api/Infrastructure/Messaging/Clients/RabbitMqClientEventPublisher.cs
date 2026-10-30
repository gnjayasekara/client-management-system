
using System.Text.Json;
using ClientManagement.Api.Features.Clients.Events;
using RabbitMQ.Client;

namespace ClientManagement.Api.Infrastructure.Messaging.Clients;

public sealed class RabbitMqClientEventPublisher
    : IClientEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqClientEventPublisher> _logger;

    public RabbitMqClientEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqClientEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishCreatedAsync(
        ClientCreatedEvent message,
        CancellationToken ct)
    {
        return PublishAsync(
            "cms.clients.created", message, message.EventId, ct);
    }

    public Task PublishUpdatedAsync(
        ClientUpdatedEvent message,
        CancellationToken ct)
    {
        return PublishAsync(
            "cms.clients.updated", message, message.EventId, ct);
    }

    public Task PublishDeletedAsync(
        ClientDeletedEvent message,
        CancellationToken ct)
    {
        return PublishAsync(
            "cms.clients.deleted", message, message.EventId, ct);
    }

    private async Task PublishAsync<T>(
        string queueName,
        T message,
        Guid eventId,
        CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"]
                ?? "localhost",
            Port = _configuration.GetValue<int?>("RabbitMq:Port")
                ?? 5672,
            UserName = _configuration["RabbitMq:UserName"]
                ?? "guest",
            Password = _configuration["RabbitMq:Password"]
                ?? "guest"
        };

        await using var connection =
            await factory.CreateConnectionAsync(ct);

        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        await using var channel =
            await connection.CreateChannelAsync(options, ct);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = eventId.ToString()
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Published RabbitMQ event {EventType} with ID {EventId} to {Queue}",
            typeof(T).Name,
            eventId,
            queueName);
    }
}
