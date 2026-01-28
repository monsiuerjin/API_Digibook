using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class Category
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("icon")]
        public string Icon { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;
    }
}
