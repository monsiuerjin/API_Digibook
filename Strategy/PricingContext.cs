using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Strategy
{
    /// <summary>
    /// Context class that uses a pricing strategy
    /// Allows switching between different pricing strategies at runtime
    /// </summary>
    public class PricingContext
    {
        private IPricingStrategy _strategy;

        public PricingContext(IPricingStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// Set or change the pricing strategy
        /// </summary>
        public void SetStrategy(IPricingStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// Execute the pricing calculation using current strategy
        /// </summary>
        public double ExecuteStrategy(double basePrice, int quantity)
        {
            return _strategy.CalculatePrice(basePrice, quantity);
        }

        /// <summary>
        /// Get current strategy name
        /// </summary>
        public string GetCurrentStrategyName()
        {
            return _strategy.GetStrategyName();
        }

        /// <summary>
        /// Get current strategy description
        /// </summary>
        public string GetCurrentStrategyDescription()
        {
            return _strategy.GetDescription();
        }
    }
}
