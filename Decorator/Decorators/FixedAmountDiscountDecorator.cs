using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Concrete Decorator - Fixed amount discount (e.g., -50,000 VND)
    /// </summary>
    public class FixedAmountDiscountDecorator : DiscountDecorator
    {
        private readonly double _discountAmount;
        private readonly string _reason;

        public FixedAmountDiscountDecorator(
            IPriceCalculator priceCalculator, 
            double discountAmount,
            string reason = "Fixed Discount")
            : base(priceCalculator)
        {
            _discountAmount = discountAmount;
            _reason = reason;
        }

        public override double Calculate()
        {
            var basePrice = _priceCalculator.Calculate();
            var finalPrice = basePrice - _discountAmount;
            return finalPrice > 0 ? finalPrice : 0; // Don't go below 0
        }

        public override string GetDescription()
        {
            return $"{_priceCalculator.GetDescription()}\n" +
                   $"  - {_reason}: -{_discountAmount:N0} VND";
        }
    }
}
