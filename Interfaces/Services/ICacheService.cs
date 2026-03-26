namespace API_DigiBook.Interfaces.Services
{
    public interface ICacheService
    {
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        void Invalidate(string key);
        void InvalidateByPrefix(string prefix);
        string GetVersionedKey(string baseKey);
        void BumpVersion(string entityType);
    }
}
