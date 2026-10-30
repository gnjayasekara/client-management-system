
using System.Text.Json;
using ClientManagement.Api.Features.FollowUps.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.FollowUps;

public sealed class FollowUpCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FollowUpCreatedConsumer> _logger;

    public FollowUpCreatedConsumer(
        IConfiguration configuration,
        ILogger<FollowUpCreatedConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RabbitMQ consumer disconnected or failed. Retrying."
                );
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ConsumeAsync(
        CancellationToken stoppingToken)
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
                ?? "guest",

            AutomaticRecoveryEnabled = false
        };

        await using var connection =
            await factory.CreateConnectionAsync(stoppingToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

        const string queueName = "cms.followups.created";

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                
                var message =
                    JsonSerializer.Deserialize<FollowUpCreatedEvent>(
                        ea.Body.Span);

                if (message is null)
                {
                    throw new JsonException(
                        "RabbitMQ message body was null.");
                }

                _logger.LogInformation(
                    "Received FollowUpCreated event. " +
                    "EventId: {EventId}, FollowUpId: {FollowUpId}, " +
                    "ClientId: {ClientId}",
                    message.EventId,
                    message.FollowUpId,
                    message.ClientId
                );

                
                await channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false
                );
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid FollowUpCreated message. " +
                    "Rejecting without requeue."
                );

                
                await channel.BasicRejectAsync(
                    deliveryTag: ea.DeliveryTag,
                    requeue: false
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing FollowUpCreated message."
                );

                // Requeue after transient processing failure.
                await channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: true
                );
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation(
            "RabbitMQ consumer started. Listening on {Queue}",
            queueName
        );

        
        while (!stoppingToken.IsCancellationRequested &&
               connection.IsOpen &&
               channel.IsOpen)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(2),
                stoppingToken);
        }
    }
}
