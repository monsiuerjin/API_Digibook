using API_DigiBook.Models;

namespace API_DigiBook.Repositories
{
    public interface ICouponRepository : IRepository<Coupon>
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task<IEnumerable<Coupon>> GetActiveAsync();
        Task<bool> IncrementUsageAsync(string id);
    }
}
