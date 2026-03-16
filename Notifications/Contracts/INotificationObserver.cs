using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Notifications.Contracts
{
    public interface INotificationObserver
    {
        string ObserverName { get; }
        bool CanHandle(string eventType);
        Task HandleAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
    }
}
