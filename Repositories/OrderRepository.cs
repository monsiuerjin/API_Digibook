using API_DigiBook.Models;
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
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("userId", userId)
                    .OrderByDescending("createdAt");
                
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
                _logger?.LogError(ex, "Error getting orders by user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(string status)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("status", status)
                    .OrderByDescending("createdAt");
                
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
