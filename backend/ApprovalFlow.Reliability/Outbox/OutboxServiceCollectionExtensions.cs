using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApprovalFlow.Reliability.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddDaprOutbox(
        this IServiceCollection services,
        Action<OutboxOptions> configure)
    {
        services.Configure(configure);
        services.TryAddSingleton<IOutboxStore, DaprOutboxStore>();
        services.TryAddSingleton<OutboxService>();
        services.TryAddSingleton<IOutboxService>(sp => sp.GetRequiredService<OutboxService>());
        services.AddHostedService<OutboxBackgroundPublisher>();
        return services;
    }
}
