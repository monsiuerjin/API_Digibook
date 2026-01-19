using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [FirestoreProperty("address")]
        public string Address { get; set; } = string.Empty;

        [FirestoreProperty("avatar")]
        public string Avatar { get; set; } = string.Empty;

        [FirestoreProperty("bio")]
        public string Bio { get; set; } = string.Empty;

        [FirestoreProperty("gender")]
        public string Gender { get; set; } = string.Empty;

        [FirestoreProperty("birthday")]
        public string Birthday { get; set; } = string.Empty;

        [FirestoreProperty("role")]
        public string Role { get; set; } = "user";

        [FirestoreProperty("status")]
        public string Status { get; set; } = "active";

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
