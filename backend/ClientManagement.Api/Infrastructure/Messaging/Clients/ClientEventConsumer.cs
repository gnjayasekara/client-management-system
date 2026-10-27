
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClientManagement.Api.Infrastructure.Messaging.Clients;

public sealed class ClientEventConsumer<TEvent> : BackgroundService
{
    private readonly string _queueName;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClientEventConsumer<TEvent>> _logger;

    public ClientEventConsumer(
        string queueName,
        IConfiguration configuration,
        ILogger<ClientEventConsumer<TEvent>> logger)
    {
        _queueName = queueName;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = _configuration.GetValue("RabbitMq:Port", 5672),
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            AutomaticRecoveryEnabled = false
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection =
                    await factory.CreateConnectionAsync(stoppingToken);

                await using var channel =
                    await connection.CreateChannelAsync(
                        cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<TEvent>(
                            delivery.Body.Span);

                        if (message is null)
                            throw new JsonException("Empty event payload.");

                        _logger.LogInformation(
                            "Received {EventType} event: {@Event}",
                            typeof(TEvent).Name,
                            message);

                        await channel.BasicAckAsync(
                            delivery.DeliveryTag,
                            multiple: false);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Invalid event in {Queue}",
                            _queueName);

                        await channel.BasicRejectAsync(
                            delivery.DeliveryTag,
                            requeue: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to process event in {Queue}",
                            _queueName);

                        // Temporary local-development behavior.
                        // No DLQ is configured yet.
                        await channel.BasicNackAsync(
                            delivery.DeliveryTag,
                            multiple: false,
                            requeue: false);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "Client event consumer listening on {Queue}",
                    _queueName);

                while (!stoppingToken.IsCancellationRequested &&
                       connection.IsOpen &&
                       channel.IsOpen)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Consumer connection failed: {Queue}", _queueName);
            }

            try
            {
                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
