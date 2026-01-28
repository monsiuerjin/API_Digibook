using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetByRoleAsync(string role);
        Task<IEnumerable<User>> GetByStatusAsync(string status);
        
        // Address management
        Task<bool> AddAddressAsync(string userId, Address address);
        Task<bool> UpdateAddressAsync(string userId, string addressId, Address address);
        Task<bool> DeleteAddressAsync(string userId, string addressId);
        Task<bool> SetDefaultAddressAsync(string userId, string addressId);
        
        // Wishlist management
        Task<bool> AddToWishlistAsync(string userId, string bookId);
        Task<bool> RemoveFromWishlistAsync(string userId, string bookId);
        Task<List<string>> GetWishlistAsync(string userId);
    }
}
