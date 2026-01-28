using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Concrete Decorator - Bulk purchase discount (buy more, save more)
    /// </summary>
    public class BulkPurchaseDiscountDecorator : DiscountDecorator
    {
        private readonly int _quantity;
        private readonly Dictionary<int, double> _quantityDiscounts = new()
        {
            { 3, 5 },    // 3-4 items: 5% off
            { 5, 10 },   // 5-9 items: 10% off
            { 10, 15 },  // 10-19 items: 15% off
            { 20, 20 }   // 20+ items: 20% off
        };

        public BulkPurchaseDiscountDecorator(
            IPriceCalculator priceCalculator, 
            int quantity)
            : base(priceCalculator)
        {
            _quantity = quantity;
        }

        public override double Calculate()
        {
            var basePrice = _priceCalculator.Calculate();
            var discountPercentage = GetDiscountPercentage();
            
            if (discountPercentage > 0)
            {
                var discount = basePrice * (discountPercentage / 100);
                return basePrice - discount;
            }
            
            return basePrice;
        }

        public override string GetDescription()
        {
            var basePrice = _priceCalculator.Calculate();
            var discountPercentage = GetDiscountPercentage();
            
            if (discountPercentage > 0)
            {
                var finalPrice = Calculate();
                var discountAmount = basePrice - finalPrice;
                
                return $"{_priceCalculator.GetDescription()}\n" +
                       $"  - Bulk Purchase ({_quantity} items, {discountPercentage}%): -{discountAmount:N0} VND";
            }
            
            return _priceCalculator.GetDescription();
        }

        private double GetDiscountPercentage()
        {
            // Find applicable discount based on quantity
            var sortedThresholds = _quantityDiscounts.Keys.OrderByDescending(k => k);
            
            foreach (var threshold in sortedThresholds)
            {
                if (_quantity >= threshold)
                {
                    return _quantityDiscounts[threshold];
                }
            }
            
            return 0;
        }
    }
}
