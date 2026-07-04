using System.Collections.Concurrent;
using NotificationService.Models;

namespace NotificationService.Services;

public sealed class InMemoryNotificationStore : INotificationStore
{
    private readonly ConcurrentDictionary<string, NotificationRecord> records = new(StringComparer.Ordinal);

    public Task<NotificationRecord?> GetAsync(string trackingId, string outcomeType)
    {
        records.TryGetValue(BuildKey(trackingId, outcomeType), out var record);
        return Task.FromResult(record);
    }

    public Task SaveAsync(NotificationRecord record)
    {
        records[BuildKey(record.TrackingId, record.OutcomeType)] = record;
        return Task.CompletedTask;
    }

    private static string BuildKey(string trackingId, string outcomeType) =>
        $"{trackingId}:{outcomeType}";
}
