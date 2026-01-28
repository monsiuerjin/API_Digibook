using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Concrete Decorator - Percentage discount (e.g., 10%, 20%)
    /// </summary>
    public class PercentageDiscountDecorator : DiscountDecorator
    {
        private readonly double _discountPercentage;
        private readonly string _reason;

        public PercentageDiscountDecorator(
            IPriceCalculator priceCalculator, 
            double discountPercentage,
            string reason = "Percentage Discount")
            : base(priceCalculator)
        {
            _discountPercentage = discountPercentage;
            _reason = reason;
        }

        public override double Calculate()
        {
            var basePrice = _priceCalculator.Calculate();
            var discount = basePrice * (_discountPercentage / 100);
            return basePrice - discount;
        }

        public override string GetDescription()
        {
            var basePrice = _priceCalculator.Calculate();
            var finalPrice = Calculate();
            var discountAmount = basePrice - finalPrice;
            
            return $"{_priceCalculator.GetDescription()}\n" +
                   $"  - {_reason} ({_discountPercentage}%): -{discountAmount:N0} VND";
        }
    }
}
