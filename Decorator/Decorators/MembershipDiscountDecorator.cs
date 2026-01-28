using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Concrete Decorator - Membership tier discount
    /// </summary>
    public class MembershipDiscountDecorator : DiscountDecorator
    {
        private readonly string _membershipTier;
        private readonly Dictionary<string, double> _tierDiscounts = new()
        {
            { "BRONZE", 5 },
            { "SILVER", 10 },
            { "GOLD", 15 },
            { "PLATINUM", 20 }
        };

        public MembershipDiscountDecorator(
            IPriceCalculator priceCalculator, 
            string membershipTier)
            : base(priceCalculator)
        {
            _membershipTier = membershipTier.ToUpper();
        }

        public override double Calculate()
        {
            var basePrice = _priceCalculator.Calculate();
            
            if (_tierDiscounts.TryGetValue(_membershipTier, out var discountPercentage))
            {
                var discount = basePrice * (discountPercentage / 100);
                return basePrice - discount;
            }
            
            return basePrice; // No discount for unknown tiers
        }

        public override string GetDescription()
        {
            var basePrice = _priceCalculator.Calculate();
            var finalPrice = Calculate();
            var discountAmount = basePrice - finalPrice;
            
            if (discountAmount > 0)
            {
                var percentage = _tierDiscounts[_membershipTier];
                return $"{_priceCalculator.GetDescription()}\n" +
                       $"  - {_membershipTier} Member Discount ({percentage}%): -{discountAmount:N0} VND";
            }
            
            return _priceCalculator.GetDescription();
        }
    }
}
