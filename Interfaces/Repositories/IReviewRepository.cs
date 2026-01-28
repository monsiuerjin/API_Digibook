using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Repositories
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<IEnumerable<Review>> GetByBookIdAsync(string bookId);
        Task<IEnumerable<Review>> GetByUserIdAsync(string userId);
        Task<double> GetAverageRatingByBookIdAsync(string bookId);
    }
}
