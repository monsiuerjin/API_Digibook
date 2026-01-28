using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Strategy.Strategies
{
    /// <summary>
    /// Member pricing strategy - 10% discount for members
    /// </summary>
    public class MemberPricingStrategy : IPricingStrategy
    {
        private const double MEMBER_DISCOUNT_RATE = 0.10; // 10%

        public double CalculatePrice(double basePrice, int quantity)
        {
            double totalPrice = basePrice * quantity;
            double discount = totalPrice * MEMBER_DISCOUNT_RATE;
            return totalPrice - discount;
        }

        public string GetStrategyName()
        {
            return "Member Pricing";
        }

        public string GetDescription()
        {
            return $"Member discount of {MEMBER_DISCOUNT_RATE * 100}% applied. Price = (Base Price × Quantity) - 10%";
        }
    }
}
