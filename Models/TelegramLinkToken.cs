using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class TelegramLinkToken
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("token")]
        public string Token { get; set; } = string.Empty;

        [FirestoreProperty("isUsed")]
        public bool IsUsed { get; set; }

        [FirestoreProperty("expiresAt")]
        public Timestamp ExpiresAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));

        [FirestoreProperty("usedAt")]
        public Timestamp? UsedAt { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
    }
}
