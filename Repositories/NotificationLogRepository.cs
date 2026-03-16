using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Repositories
{
    public class NotificationLogRepository : FirestoreRepository<NotificationLog>, INotificationLogRepository
    {
        public NotificationLogRepository(ILogger<NotificationLogRepository> logger)
            : base("notificationLogs", logger)
        {
        }

        public async Task<bool> HasSentAsync(string idempotencyKey)
        {
            var query = _db.Collection(_collectionName)
                .WhereEqualTo("idempotencyKey", idempotencyKey)
                .WhereEqualTo("status", "Sent")
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Any();
        }
    }
}
