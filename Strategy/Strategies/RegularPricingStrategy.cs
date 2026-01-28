using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Strategy.Strategies
{
    /// <summary>
    /// Regular pricing strategy - No discounts applied
    /// </summary>
    public class RegularPricingStrategy : IPricingStrategy
    {
        public double CalculatePrice(double basePrice, int quantity)
        {
            return basePrice * quantity;
        }

        public string GetStrategyName()
        {
            return "Regular Pricing";
        }

        public string GetDescription()
        {
            return "Standard pricing without any discounts. Price = Base Price × Quantity";
        }
    }
}
