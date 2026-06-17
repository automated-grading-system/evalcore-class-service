using System.Text;
using Class.Infrastructure.Configuration;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Class.Infrastructure.Messaging;

public sealed class OutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxPublisherService(
        ILogger<OutboxPublisherService> logger,
        RabbitMqOptions options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _options = options;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox publisher failed. Unpublished events will be retried later.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ClassDbContext>();
        var events = await dbContext.OutboxEvents
            .Where(x => x.PublishedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        using var connection = factory.CreateConnection("class-service-outbox");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        foreach (var outboxEvent in events)
        {
            try
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = outboxEvent.Id.ToString();
                properties.Type = outboxEvent.EventType;
                properties.Timestamp = new AmqpTimestamp(outboxEvent.OccurredAt.ToUnixTimeSeconds());

                var body = Encoding.UTF8.GetBytes(outboxEvent.PayloadJson);
                channel.BasicPublish(
                    exchange: _options.Exchange,
                    routingKey: outboxEvent.RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                outboxEvent.PublishedAt = DateTimeOffset.UtcNow;
                outboxEvent.LastError = null;
            }
            catch (Exception ex)
            {
                outboxEvent.PublishAttempts += 1;
                outboxEvent.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                _logger.LogWarning(ex, "Failed to publish outbox event {OutboxEventId}.", outboxEvent.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
