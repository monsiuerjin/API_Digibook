using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Services
{
    public interface IChatRecommendationService
    {
        Task<ChatRecommendationResponse> GetRecommendationAsync(ChatRecommendationRequest request, CancellationToken cancellationToken = default);
    }
}
