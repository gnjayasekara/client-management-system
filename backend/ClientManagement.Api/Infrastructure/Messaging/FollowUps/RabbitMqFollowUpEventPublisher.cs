
using System.Text.Json;
using ClientManagement.Api.Features.FollowUps.Events;
using RabbitMQ.Client;

namespace ClientManagement.Api.Infrastructure.Messaging.FollowUps;

public sealed class RabbitMqFollowUpEventPublisher
    : IFollowUpEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqFollowUpEventPublisher> _logger;

    public RabbitMqFollowUpEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqFollowUpEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishCreatedAsync(
        FollowUpCreatedEvent message,
        CancellationToken ct) =>
        PublishAsync(
            "cms.followups.created", message, message.EventId, ct);

    public Task PublishUpdatedAsync(
        FollowUpUpdatedEvent message,
        CancellationToken ct) =>
        PublishAsync(
            "cms.followups.updated", message, message.EventId, ct);

    public Task PublishCompletedAsync(
        FollowUpCompletedEvent message,
        CancellationToken ct) =>
        PublishAsync(
            "cms.followups.completed", message, message.EventId, ct);

    public Task PublishDeletedAsync(
        FollowUpDeletedEvent message,
        CancellationToken ct) =>
        PublishAsync(
            "cms.followups.deleted", message, message.EventId, ct);

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
            Port = _configuration.GetValue<int?>(
                "RabbitMq:Port") ?? 5672,
            UserName = _configuration["RabbitMq:UserName"]
                ?? "guest",
            Password = _configuration["RabbitMq:Password"]
                ?? "guest"
        };

        await using var connection =
            await factory.CreateConnectionAsync(ct);

        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

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
            "Published {EventType} {EventId} to {Queue}",
            typeof(T).Name,
            eventId,
            queueName);
    }
}
