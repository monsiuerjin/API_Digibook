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

        [FirestoreProperty("originalPrice")]
        public double OriginalPrice { get; set; }

        [FirestoreProperty("stockQuantity")]
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

        [FirestoreProperty("isAvailable")]
        public bool IsAvailable { get; set; } = true;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }

        // Phase 1 Upgrade Fields
        [FirestoreProperty("slug")]
        public string Slug { get; set; } = string.Empty;

        [FirestoreProperty("viewCount")]
        public int ViewCount { get; set; } = 0;

        [FirestoreProperty("searchKeywords")]
        public List<string> SearchKeywords { get; set; } = new List<string>();

        [FirestoreProperty("reviewCount")]
        public int ReviewCount { get; set; } = 0;

        // Tiki Integration Fields
        [FirestoreProperty("quantitySold")]
        public QuantitySold? QuantitySold { get; set; }

        [FirestoreProperty("badges")]
        public List<BookBadge> Badges { get; set; } = new List<BookBadge>();

        [FirestoreProperty("discountRate")]
        public double DiscountRate { get; set; } = 0;

        // Phase 2 Upgrade - Rich Data Fields
        [FirestoreProperty("images")]
        public List<string> Images { get; set; } = new List<string>();

        [FirestoreProperty("dimensions")]
        public string Dimensions { get; set; } = string.Empty;

        [FirestoreProperty("translator")]
        public string Translator { get; set; } = string.Empty;

        [FirestoreProperty("bookLayout")]
        public string BookLayout { get; set; } = string.Empty;

        [FirestoreProperty("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;
    }

    [FirestoreData]
    public class QuantitySold
    {
        [FirestoreProperty("text")]
        public string Text { get; set; } = string.Empty;

        [FirestoreProperty("value")]
        public int Value { get; set; }
    }

    [FirestoreData]
    public class BookBadge
    {
        [FirestoreProperty("code")]
        public string Code { get; set; } = string.Empty;

        [FirestoreProperty("text")]
        public string Text { get; set; } = string.Empty;

        [FirestoreProperty("type")]
        public string Type { get; set; } = string.Empty;
    }
}
