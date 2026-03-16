using Google.Cloud.Firestore;

namespace API_DigiBook.Notifications.Models
{
    [FirestoreData]
    public class NotificationLog
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("eventId")]
        public string EventId { get; set; } = string.Empty;

        [FirestoreProperty("eventType")]
        public string EventType { get; set; } = string.Empty;

        [FirestoreProperty("channel")]
        public string Channel { get; set; } = string.Empty;

        [FirestoreProperty("recipient")]
        public string Recipient { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = "Pending";

        [FirestoreProperty("idempotencyKey")]
        public string IdempotencyKey { get; set; } = string.Empty;

        [FirestoreProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;

        [FirestoreProperty("providerResponse")]
        public string ProviderResponse { get; set; } = string.Empty;

        [FirestoreProperty("attempt")]
        public int Attempt { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; } = Timestamp.GetCurrentTimestamp();

        [FirestoreProperty("sentAt")]
        public Timestamp? SentAt { get; set; }
    }
}
