using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace API_DigiBook.Repositories
{
    public class UserRepository : FirestoreRepository<User>, IUserRepository
    {
        public UserRepository(ILogger<UserRepository> logger) 
            : base("users", logger)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("email", email)
                    .Limit(1);
                
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Documents.Count == 0)
                {
                    return null;
                }

                var document = snapshot.Documents[0];
                var user = document.ConvertTo<User>();
                user.Id = document.Id;
                return user;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user by email {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string role)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("role", role);
                
                var snapshot = await query.GetSnapshotAsync();
                var users = new List<User>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var user = document.ConvertTo<User>();
                        user.Id = document.Id;
                        users.Add(user);
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting users by role {Role}", role);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetByStatusAsync(string status)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("status", status);
                
                var snapshot = await query.GetSnapshotAsync();
                var users = new List<User>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var user = document.ConvertTo<User>();
                        user.Id = document.Id;
                        users.Add(user);
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting users by status {Status}", status);
                throw;
            }
        }

        // Address Management
        public async Task<bool> AddAddressAsync(string userId, Address address)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                var user = snapshot.ConvertTo<User>();
                address.Id = Guid.NewGuid().ToString();
                
                // If this is the first address or marked as default, set it as default
                if (!user.Addresses.Any() || address.IsDefault)
                {
                    // Set all other addresses to non-default
                    foreach (var addr in user.Addresses)
                    {
                        addr.IsDefault = false;
                    }
                    address.IsDefault = true;
                }
                
                user.Addresses.Add(address);
                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                await docRef.SetAsync(user, SetOptions.MergeAll);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding address for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateAddressAsync(string userId, string addressId, Address address)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                var user = snapshot.ConvertTo<User>();
                var existingAddress = user.Addresses.FirstOrDefault(a => a.Id == addressId);

                if (existingAddress == null)
                {
                    return false;
                }

                // Update the address
                existingAddress.Label = address.Label;
                existingAddress.RecipientName = address.RecipientName;
                existingAddress.Phone = address.Phone;
                existingAddress.FullAddress = address.FullAddress;
                
                if (address.IsDefault)
                {
                    // Set all other addresses to non-default
                    foreach (var addr in user.Addresses)
                    {
                        addr.IsDefault = false;
                    }
                    existingAddress.IsDefault = true;
                }

                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                await docRef.SetAsync(user, SetOptions.MergeAll);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating address {AddressId} for user {UserId}", addressId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteAddressAsync(string userId, string addressId)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                var user = snapshot.ConvertTo<User>();
                var addressToRemove = user.Addresses.FirstOrDefault(a => a.Id == addressId);

                if (addressToRemove == null)
                {
                    return false;
                }

                user.Addresses.Remove(addressToRemove);
                
                // If removed address was default and there are other addresses, set the first one as default
                if (addressToRemove.IsDefault && user.Addresses.Any())
                {
                    user.Addresses[0].IsDefault = true;
                }

                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                await docRef.SetAsync(user, SetOptions.MergeAll);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting address {AddressId} for user {UserId}", addressId, userId);
                throw;
            }
        }

        public async Task<bool> SetDefaultAddressAsync(string userId, string addressId)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                var user = snapshot.ConvertTo<User>();
                var address = user.Addresses.FirstOrDefault(a => a.Id == addressId);

                if (address == null)
                {
                    return false;
                }

                // Set all addresses to non-default
                foreach (var addr in user.Addresses)
                {
                    addr.IsDefault = false;
                }
                
                // Set the specified address as default
                address.IsDefault = true;
                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                await docRef.SetAsync(user, SetOptions.MergeAll);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting default address {AddressId} for user {UserId}", addressId, userId);
                throw;
            }
        }

        // Wishlist Management
        public async Task<bool> AddToWishlistAsync(string userId, string bookId)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                var user = snapshot.ConvertTo<User>();
                
                if (!user.WishlistIds.Contains(bookId))
                {
                    user.WishlistIds.Add(bookId);
                    user.UpdatedAt = Timestamp.GetCurrentTimestamp();
                    await docRef.SetAsync(user, SetOptions.MergeAll);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding book {BookId} to wishlist for user {UserId}", bookId, userId);
                throw;
            }
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, string bookId)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                var user = snapshot.ConvertTo<User>();
                
                if (user.WishlistIds.Contains(bookId))
                {
                    user.WishlistIds.Remove(bookId);
                    user.UpdatedAt = Timestamp.GetCurrentTimestamp();
                    await docRef.SetAsync(user, SetOptions.MergeAll);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing book {BookId} from wishlist for user {UserId}", bookId, userId);
                throw;
            }
        }

        public async Task<List<string>> GetWishlistAsync(string userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                
                if (user == null)
                {
                    return new List<string>();
                }

                return user.WishlistIds;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting wishlist for user {UserId}", userId);
                throw;
            }
        }
    }
}
