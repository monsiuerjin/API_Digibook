using API_DigiBook.Models;

namespace API_DigiBook.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Order>> GetByStatusAsync(string status);
        Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10);
    }
}
