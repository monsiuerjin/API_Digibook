using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class SystemLog
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("action")]
        public string Action { get; set; } = string.Empty;

        [FirestoreProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = "SUCCESS";

        [FirestoreProperty("user")]
        public string User { get; set; } = "Anonymous";

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }
    }
}
