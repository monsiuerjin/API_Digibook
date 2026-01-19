using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class Book
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("author")]
        public string Author { get; set; } = string.Empty;

        [FirestoreProperty("authorId")]
        public string AuthorId { get; set; } = string.Empty;

        [FirestoreProperty("authorBio")]
        public string AuthorBio { get; set; } = string.Empty;

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty;

        [FirestoreProperty("price")]
        public double Price { get; set; }

        [FirestoreProperty("original_price")]
        public double OriginalPrice { get; set; }

        [FirestoreProperty("stock_quantity")]
        public int StockQuantity { get; set; }

        [FirestoreProperty("rating")]
        public double Rating { get; set; }

        [FirestoreProperty("cover")]
        public string Cover { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("isbn")]
        public string Isbn { get; set; } = string.Empty;

        [FirestoreProperty("pages")]
        public int Pages { get; set; }

        [FirestoreProperty("publisher")]
        public string Publisher { get; set; } = string.Empty;

        [FirestoreProperty("publishYear")]
        public int PublishYear { get; set; }

        [FirestoreProperty("language")]
        public string Language { get; set; } = "Tiếng Việt";

        [FirestoreProperty("badge")]
        public string Badge { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
