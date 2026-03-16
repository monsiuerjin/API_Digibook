using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Interfaces.Repositories
{
    public interface INotificationLogRepository : IRepository<NotificationLog>
    {
        Task<bool> HasSentAsync(string idempotencyKey);
    }
}
