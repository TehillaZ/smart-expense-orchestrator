using NotificationService.Models;

namespace NotificationService.Services;

public interface INotificationStore
{
    Task<NotificationRecord?> GetAsync(string trackingId, string outcomeType);

    Task SaveAsync(NotificationRecord record);
}
