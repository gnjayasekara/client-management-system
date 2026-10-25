
using System.Text;
using System.Text.Json;
using ClientManagement.Api.Features.Notes.Events;
using RabbitMQ.Client;

namespace ClientManagement.Api.Infrastructure.Messaging.Notes;

public sealed class RabbitMqNoteEventPublisher
    : INoteEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqNoteEventPublisher> _logger;

    public RabbitMqNoteEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqNoteEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    

    public async Task PublishCreatedAsync(
        NoteCreatedEvent message,
        CancellationToken cancellationToken)
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
            await factory.CreateConnectionAsync(
                cancellationToken);

        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        await using var channel =
            await connection.CreateChannelAsync(
                options,
                cancellationToken);

        const string queueName = "cms.notes.created";

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(message)
        );

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.EventId.ToString()
        };

        _logger.LogInformation(
            "REAL RabbitMQ publisher sending NoteCreated {EventId} to {Queue}",
            message.EventId,
            queueName
        );

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "REAL RabbitMQ publisher completed NoteCreated {EventId}",
            message.EventId
        );
    }
}
