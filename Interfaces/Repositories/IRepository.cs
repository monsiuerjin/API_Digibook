using System.Linq.Expressions;

namespace API_DigiBook.Interfaces.Repositories
{
    /// <summary>
    /// Generic repository interface for CRUD operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Get all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Get entity by ID
        /// </summary>
        Task<T?> GetByIdAsync(string id);

        /// <summary>
        /// Add new entity
        /// </summary>
        Task<string> AddAsync(T entity, string? customId = null);

        /// <summary>
        /// Update existing entity
        /// </summary>
        Task<bool> UpdateAsync(string id, T entity);

        /// <summary>
        /// Update specific fields
        /// </summary>
        Task<bool> UpdateFieldsAsync(string id, Dictionary<string, object> updates);

        /// <summary>
        /// Delete entity
        /// </summary>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Check if entity exists
        /// </summary>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Count total entities
        /// </summary>
        Task<int> CountAsync();

        /// <summary>
        /// Find entities with condition
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    }
}
