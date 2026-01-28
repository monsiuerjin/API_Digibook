using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Concrete Decorator - Seasonal/Holiday discount
    /// </summary>
    public class SeasonalDiscountDecorator : DiscountDecorator
    {
        private readonly string _seasonName;
        private readonly double _discountPercentage;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;

        public SeasonalDiscountDecorator(
            IPriceCalculator priceCalculator, 
            string seasonName,
            double discountPercentage,
            DateTime startDate,
            DateTime endDate)
            : base(priceCalculator)
        {
            _seasonName = seasonName;
            _discountPercentage = discountPercentage;
            _startDate = startDate;
            _endDate = endDate;
        }

        public override double Calculate()
        {
            var basePrice = _priceCalculator.Calculate();
            var now = DateTime.Now;
            
            // Only apply discount if within date range
            if (now >= _startDate && now <= _endDate)
            {
                var discount = basePrice * (_discountPercentage / 100);
                return basePrice - discount;
            }
            
            return basePrice;
        }

        public override string GetDescription()
        {
            var basePrice = _priceCalculator.Calculate();
            var finalPrice = Calculate();
            var discountAmount = basePrice - finalPrice;
            var now = DateTime.Now;
            
            if (discountAmount > 0 && now >= _startDate && now <= _endDate)
            {
                return $"{_priceCalculator.GetDescription()}\n" +
                       $"  - {_seasonName} Sale ({_discountPercentage}%): -{discountAmount:N0} VND";
            }
            
            return _priceCalculator.GetDescription();
        }
    }
}
