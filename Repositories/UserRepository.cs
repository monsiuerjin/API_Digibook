using API_DigiBook.Models;
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
    }
}
