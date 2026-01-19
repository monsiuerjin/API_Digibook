using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class AISettings
    {
        [FirestoreProperty("activeModelId")]
        public string ActiveModelId { get; set; } = "gemini-1.5-flash";

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }

        [FirestoreProperty("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
