using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Strategy.Strategies
{
    /// <summary>
    /// Wholesale pricing strategy - Tiered discounts based on quantity
    /// </summary>
    public class WholesalePricingStrategy : IPricingStrategy
    {
        public double CalculatePrice(double basePrice, int quantity)
        {
            double totalPrice = basePrice * quantity;
            double discountRate = GetDiscountRate(quantity);
            double discount = totalPrice * discountRate;
            
            return totalPrice - discount;
        }

        private double GetDiscountRate(int quantity)
        {
            if (quantity >= 100)
                return 0.25; // 25% discount for 100+
            else if (quantity >= 50)
                return 0.20; // 20% discount for 50-99
            else if (quantity >= 20)
                return 0.15; // 15% discount for 20-49
            else if (quantity >= 10)
                return 0.10; // 10% discount for 10-19
            else
                return 0.05; // 5% discount for < 10
        }

        public string GetStrategyName()
        {
            return "Wholesale Pricing";
        }

        public string GetDescription()
        {
            return "Bulk purchase discounts: 5% (1-9), 10% (10-19), 15% (20-49), 20% (50-99), 25% (100+)";
        }
    }
}
