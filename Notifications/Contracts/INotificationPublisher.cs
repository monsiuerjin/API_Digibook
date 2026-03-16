using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Notifications.Contracts
{
    public interface INotificationPublisher
    {
        Task PublishAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
    }
}
