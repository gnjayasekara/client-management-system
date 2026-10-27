
using System.Text.Json;
using ClientManagement.Api.Features.Clients.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.Clients;

public sealed class ClientCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClientCreatedConsumer> _logger;

    public ClientCreatedConsumer(
        IConfiguration configuration,
        ILogger<ClientCreatedConsumer> logger)
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

        const string queueName = "cms.clients.created";

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
                    JsonSerializer.Deserialize<ClientCreatedEvent>(
                        ea.Body.Span);

                if (message is null)
                {
                    throw new JsonException(
                        "RabbitMQ message body was null.");
                }

                _logger.LogInformation(
                    "Received ClientCreated event. " +
                    "EventId: {EventId}  " +
                    "ClientId: {ClientId}",
                    message.EventId,
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
                    "Invalid ClientCreated message. " +
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
                    "Error processing ClientCreated message."
                );

                // Requeue after transient processing failure.
                await channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false
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
