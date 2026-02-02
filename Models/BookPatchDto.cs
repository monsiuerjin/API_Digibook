namespace API_DigiBook.Models
{
    /// <summary>
    /// Data Transfer Object for partial book updates (PATCH)
    /// All properties are nullable to allow partial updates
    /// </summary>
    public class BookPatchDto
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? AuthorId { get; set; }
        public string? AuthorBio { get; set; }
        public string? Category { get; set; }
        public double? Price { get; set; }
        public double? OriginalPrice { get; set; }
        public int? StockQuantity { get; set; }
        public double? Rating { get; set; }
        public string? Cover { get; set; }
        public string? Description { get; set; }
        public string? Isbn { get; set; }
        public int? Pages { get; set; }
        public string? Publisher { get; set; }
        public int? PublishYear { get; set; }
        public string? Language { get; set; }
        public string? Badge { get; set; }
        public bool? IsAvailable { get; set; }
        public string? Slug { get; set; }
        public int? ViewCount { get; set; }
        public List<string>? SearchKeywords { get; set; }
        public int? ReviewCount { get; set; }
        public double? DiscountRate { get; set; }
        public List<string>? Images { get; set; }
        public string? Dimensions { get; set; }
        public string? Translator { get; set; }
        public string? BookLayout { get; set; }
        public string? Manufacturer { get; set; }
    }
}
