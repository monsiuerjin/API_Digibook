using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator
{
    /// <summary>
    /// Concrete Component - Base price without any discounts
    /// </summary>
    public class BasePriceCalculator : IPriceCalculator
    {
        private readonly double _basePrice;
        private readonly string _itemName;

        public BasePriceCalculator(double basePrice, string itemName = "Item")
        {
            _basePrice = basePrice;
            _itemName = itemName;
        }

        public double Calculate()
        {
            return _basePrice;
        }

        public string GetDescription()
        {
            return $"{_itemName}: {_basePrice:N0} VND";
        }
    }
}
