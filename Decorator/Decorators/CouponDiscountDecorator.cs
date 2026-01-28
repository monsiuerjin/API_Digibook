using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Concrete Decorator - Coupon code discount
    /// </summary>
    public class CouponDiscountDecorator : DiscountDecorator
    {
        private readonly string _couponCode;
        private readonly double _discountValue;
        private readonly bool _isPercentage;

        public CouponDiscountDecorator(
            IPriceCalculator priceCalculator, 
            string couponCode,
            double discountValue,
            bool isPercentage = true)
            : base(priceCalculator)
        {
            _couponCode = couponCode;
            _discountValue = discountValue;
            _isPercentage = isPercentage;
        }

        public override double Calculate()
        {
            var basePrice = _priceCalculator.Calculate();
            
            if (_isPercentage)
            {
                var discount = basePrice * (_discountValue / 100);
                return basePrice - discount;
            }
            else
            {
                var finalPrice = basePrice - _discountValue;
                return finalPrice > 0 ? finalPrice : 0;
            }
        }

        public override string GetDescription()
        {
            var basePrice = _priceCalculator.Calculate();
            var finalPrice = Calculate();
            var discountAmount = basePrice - finalPrice;
            
            var discountType = _isPercentage ? $"{_discountValue}%" : $"{_discountValue:N0} VND";
            
            return $"{_priceCalculator.GetDescription()}\n" +
                   $"  - Coupon '{_couponCode}' ({discountType}): -{discountAmount:N0} VND";
        }
    }
}
