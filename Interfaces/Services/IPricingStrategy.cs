namespace API_DigiBook.Interfaces.Services
{
    /// <summary>
    /// Strategy Interface for pricing calculations
    /// Defines the contract for different pricing strategies
    /// </summary>
    public interface IPricingStrategy
    {
        /// <summary>
        /// Calculate the final price based on strategy
        /// </summary>
        /// <param name="basePrice">Original price of the product</param>
        /// <param name="quantity">Quantity being purchased</param>
        /// <returns>Final calculated price</returns>
        double CalculatePrice(double basePrice, int quantity);

        /// <summary>
        /// Get the name of the pricing strategy
        /// </summary>
        string GetStrategyName();

        /// <summary>
        /// Get description of how this strategy works
        /// </summary>
        string GetDescription();
    }
}
