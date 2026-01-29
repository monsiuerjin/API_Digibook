using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace API_DigiBook.Repositories
{
    public class OrderRepository : FirestoreRepository<Order>, IOrderRepository
    {
        public OrderRepository(ILogger<OrderRepository> logger) 
            : base("orders", logger)
        {
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
        {
            try
            {
                // Query without ordering first, as some old orders may not have createdAt
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("userId", userId);
                
                var snapshot = await query.GetSnapshotAsync();
                var orders = new List<Order>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var order = document.ConvertTo<Order>();
                        order.Id = document.Id;
                        orders.Add(order);
                    }
                }

                // Sort in memory by createdAt if available, otherwise by date string
                return orders.OrderByDescending(o => 
                    o.CreatedAt != null && o.CreatedAt != default(Timestamp) 
                        ? o.CreatedAt.ToDateTime() 
                        : DateTime.TryParse(o.Date, out var dt) ? dt : DateTime.MinValue
                ).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting orders by user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(string status)
        {
            try
            {
                // Case-insensitive status search
                var allOrders = await GetAllAsync();
                return allOrders
                    .Where(o => string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(o => o.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting orders by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .OrderByDescending("createdAt")
                    .Limit(count);
                
                var snapshot = await query.GetSnapshotAsync();
                var orders = new List<Order>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var order = document.ConvertTo<Order>();
                        order.Id = document.Id;
                        orders.Add(order);
                    }
                }

                return orders;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent orders");
                throw;
            }
        }
    }
}
