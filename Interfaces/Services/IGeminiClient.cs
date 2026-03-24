using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Services
{
    public interface IGeminiClient
    {
        Task<GeminiRecommendationResult?> GenerateRecommendationAsync(
            string query,
            IReadOnlyCollection<ChatRecommendedBook> candidates,
            CancellationToken cancellationToken = default);
    }
}
