using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Notifications
{
    public class NotificationPublisher : INotificationPublisher
    {
        private readonly IEnumerable<INotificationObserver> _observers;
        private readonly ILogger<NotificationPublisher> _logger;

        public NotificationPublisher(
            IEnumerable<INotificationObserver> observers,
            ILogger<NotificationPublisher> logger)
        {
            _observers = observers;
            _logger = logger;
        }

        public async Task PublishAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
        {
            var matchedObservers = _observers.Where(o => o.CanHandle(notificationEvent.EventType)).ToList();

            if (!matchedObservers.Any())
            {
                _logger.LogInformation(
                    "No notification observers matched EventType={EventType}, EventId={EventId}",
                    notificationEvent.EventType,
                    notificationEvent.EventId);
                return;
            }

            foreach (var observer in matchedObservers)
            {
                try
                {
                    await observer.HandleAsync(notificationEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Observer {ObserverName} failed for EventType={EventType}, EventId={EventId}",
                        observer.ObserverName,
                        notificationEvent.EventType,
                        notificationEvent.EventId);
                }
            }
        }
    }
}
