using NotificationService.Models;

namespace NotificationService.Services;

public interface INotificationChannel
{
    Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default);
}
