namespace API_DigiBook.Models
{
    /// <summary>
    /// Request model for pricing calculation
    /// </summary>
    public class PricingRequest
    {
        /// <summary>
        /// Base price of the product
        /// </summary>
        public double BasePrice { get; set; }

        /// <summary>
        /// Quantity to purchase
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Pricing strategy to use: "regular", "member", "wholesale", "vip"
        /// </summary>
        public string Strategy { get; set; } = "regular";

        /// <summary>
        /// Product name (optional, for display)
        /// </summary>
        public string? ProductName { get; set; }
    }
}
