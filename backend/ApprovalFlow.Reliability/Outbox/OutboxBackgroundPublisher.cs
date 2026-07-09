using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Reliability.Outbox;

public sealed class OutboxBackgroundPublisher : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly OutboxOptions options;
    private readonly ILogger<OutboxBackgroundPublisher> logger;

    public OutboxBackgroundPublisher(
        IServiceProvider serviceProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxBackgroundPublisher> logger)
    {
        this.serviceProvider = serviceProvider;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var outboxService = scope.ServiceProvider.GetRequiredService<OutboxService>();
                var pendingMessages = await store.GetPendingMessagesAsync(DateTimeOffset.UtcNow, stoppingToken);

                foreach (var message in pendingMessages)
                {
                    using (logger.BeginScope(new Dictionary<string, object>
                    {
                        ["CorrelationId"] = string.IsNullOrWhiteSpace(message.CorrelationId)
                            ? "none"
                            : message.CorrelationId
                    }))
                    {
                        logger.LogDebug(
                            "Dispatching pending outbox message {IdempotencyKey} for topic {Topic}. Correlation {CorrelationId}.",
                            message.IdempotencyKey,
                            message.Topic,
                            message.CorrelationId ?? "none");
                        await outboxService.TryDispatchAsync(message, stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = "outbox-publisher"
                }))
                {
                    logger.LogError(
                        ex,
                        "Outbox background publisher failed while processing pending messages. Correlation {CorrelationId}.",
                        "outbox-publisher");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
        }
    }
}
