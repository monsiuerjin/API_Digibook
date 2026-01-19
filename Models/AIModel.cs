using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class AIModel
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty;

        [FirestoreProperty("rpm")]
        public string Rpm { get; set; } = string.Empty;

        [FirestoreProperty("tpm")]
        public string Tpm { get; set; } = string.Empty;

        [FirestoreProperty("rpd")]
        public string Rpd { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
