using Google.Cloud.Firestore;
using API_DigiBook.Services;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using System.Linq.Expressions;
using System.Reflection;

namespace API_DigiBook.Repositories
{
    /// <summary>
    /// Base repository implementation for Firestore
    /// </summary>
    /// <typeparam name="T">Entity type with [FirestoreData] attribute</typeparam>
    public class FirestoreRepository<T> : IRepository<T> where T : class
    {
        protected readonly FirestoreDb _db;
        protected readonly string _collectionName;
        protected readonly ILogger? _logger;
        protected readonly ICacheService? _cache;

        public FirestoreRepository(string collectionName, ICacheService? cache = null, ILogger? logger = null)
        {
            _db = FirebaseService.GetFirestoreDb();
            _collectionName = collectionName;
            _cache = cache;
            _logger = logger;
        }

        protected string GetCacheKey(string suffix) => $"{_collectionName}:{suffix}";

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            if (_cache == null) return await GetAllFromFirestoreAsync();

            return await _cache.GetOrSetAsync(
                GetCacheKey("all"),
                () => GetAllFromFirestoreAsync(),
                TimeSpan.FromMinutes(10)
            ) ?? Enumerable.Empty<T>();
        }

        private async Task<IEnumerable<T>> GetAllFromFirestoreAsync()
        {
            try
            {
                var snapshot = await _db.Collection(_collectionName).GetSnapshotAsync();
                var items = new List<T>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var item = document.ConvertTo<T>();
                        
                        // Ensure Id is set from document ID
                        var idProperty = typeof(T).GetProperty("Id");
                        if (idProperty != null && idProperty.PropertyType == typeof(string))
                        {
                            var currentId = idProperty.GetValue(item) as string;
                            if (string.IsNullOrEmpty(currentId))
                            {
                                idProperty.SetValue(item, document.Id);
                            }
                        }
                        
                        items.Add(item);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all documents from {Collection}", _collectionName);
                throw;
            }
        }

        public virtual async Task<T?> GetByIdAsync(string id)
        {
            if (_cache == null) return await GetByIdFromFirestoreAsync(id);

            return await _cache.GetOrSetAsync(
                GetCacheKey(id),
                () => GetByIdFromFirestoreAsync(id),
                TimeSpan.FromMinutes(10)
            );
        }

        private async Task<T?> GetByIdFromFirestoreAsync(string id)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return null;
                }

                var item = snapshot.ConvertTo<T>();
                
                // Ensure Id is set from document ID
                var idProperty = typeof(T).GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(string))
                {
                    idProperty.SetValue(item, snapshot.Id);
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting document {Id} from {Collection}", id, _collectionName);
                throw;
            }
        }

        protected void ClearCache()
        {
            _cache?.InvalidateByPrefix(_collectionName);
        }

        public virtual async Task<string> AddAsync(T entity, string? customId = null)
        {
            try
            {
                var collectionRef = _db.Collection(_collectionName);
                string id;

                if (!string.IsNullOrEmpty(customId))
                {
                    var docRef = collectionRef.Document(customId);
                    await docRef.SetAsync(entity);
                    id = customId;
                }
                else
                {
                    var docRef = await collectionRef.AddAsync(entity);
                    id = docRef.Id;
                }

                ClearCache();
                return id;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding document to {Collection}", _collectionName);
                throw;
            }
        }

        public virtual async Task<bool> UpdateAsync(string id, T entity)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                await docRef.SetAsync(entity, SetOptions.MergeAll);
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating document {Id} in {Collection}", id, _collectionName);
                throw;
            }
        }

        public virtual async Task<bool> UpdateFieldsAsync(string id, Dictionary<string, object?> updates)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                await docRef.UpdateAsync(updates);
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating fields in document {Id} in {Collection}", id, _collectionName);
                throw;
            }
        }

        public virtual async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                await docRef.DeleteAsync();
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting document {Id} from {Collection}", id, _collectionName);
                throw;
            }
        }


        public virtual async Task<bool> ExistsAsync(string id)
        {
            try
            {
                Console.WriteLine($"🔥 [Firestore Hit] EXISTS check for collection: {_collectionName}, ID: {id}");
                var docRef = _db.Collection(_collectionName).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();
                return snapshot.Exists;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking existence of document {Id} in {Collection}", id, _collectionName);
                throw;
            }
        }

        public virtual async Task<int> CountAsync()
        {
            try
            {
                var snapshot = await _db.Collection(_collectionName).GetSnapshotAsync();
                return snapshot.Count;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error counting documents in {Collection}", _collectionName);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                // Note: Expression-based queries are limited in Firestore
                // For complex queries, override this method in specific repositories
                var allItems = await GetAllAsync();
                return allItems.Where(predicate.Compile());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error finding documents in {Collection}", _collectionName);
                throw;
            }
        }
    }
}
