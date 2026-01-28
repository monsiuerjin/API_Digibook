using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Decorator.Decorators
{
    /// <summary>
    /// Abstract Decorator - Base class for all discount decorators
    /// </summary>
    public abstract class DiscountDecorator : IPriceCalculator
    {
        protected readonly IPriceCalculator _priceCalculator;

        protected DiscountDecorator(IPriceCalculator priceCalculator)
        {
            _priceCalculator = priceCalculator;
        }

        public virtual double Calculate()
        {
            return _priceCalculator.Calculate();
        }

        public virtual string GetDescription()
        {
            return _priceCalculator.GetDescription();
        }
    }
}
