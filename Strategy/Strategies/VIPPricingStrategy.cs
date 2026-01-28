using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Strategy.Strategies
{
    /// <summary>
    /// VIP pricing strategy - 20% discount + additional quantity bonuses
    /// </summary>
    public class VIPPricingStrategy : IPricingStrategy
    {
        private const double VIP_DISCOUNT_RATE = 0.20; // 20% base discount

        public double CalculatePrice(double basePrice, int quantity)
        {
            double totalPrice = basePrice * quantity;
            
            // Base VIP discount 20%
            double discount = totalPrice * VIP_DISCOUNT_RATE;
            
            // Additional quantity bonus for VIP
            if (quantity >= 5)
            {
                discount += totalPrice * 0.05; // Extra 5% for 5+ items
            }
            
            if (quantity >= 10)
            {
                discount += totalPrice * 0.05; // Extra 5% for 10+ items (total 30%)
            }
            
            return totalPrice - discount;
        }

        public string GetStrategyName()
        {
            return "VIP Pricing";
        }

        public string GetDescription()
        {
            return "VIP exclusive: 20% base discount + 5% bonus (5+ items) + 5% bonus (10+ items). Max 30% off!";
        }
    }
}
