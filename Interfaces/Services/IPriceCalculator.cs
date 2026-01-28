namespace API_DigiBook.Interfaces.Services
{
    /// <summary>
    /// Base interface for price calculation (Component in Decorator Pattern)
    /// </summary>
    public interface IPriceCalculator
    {
        double Calculate();
        string GetDescription();
    }
}
