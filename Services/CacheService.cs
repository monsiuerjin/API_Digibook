using Microsoft.Extensions.Caching.Memory;
using API_DigiBook.Interfaces.Services;
using Google.Cloud.Firestore;

namespace API_DigiBook.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(2);

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? CachedValue))
            {
                return CachedValue;
            }

            T result = await factory();
            
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            _cache.Set(key, result, options);
            CacheReadMonitor.Record(key, _logger);
            
            return result;
        }

        public void Invalidate(string key)
        {
            _cache.Remove(key);
        }

        public void InvalidateByPrefix(string prefix)
        {
            // Note: IMemoryCache doesn't natively support removing by prefix
            // In a real production app, we'd use a custom dictionary to track keys
            // or use a distributed cache like Redis.
            // For now, we rely on Version-based invalidation (BumpVersion).
        }

        public string GetVersionedKey(string baseKey)
        {
            // baseKey format: books:{id} or books:all
            // Extract entity type (e.g., "books")
            var parts = baseKey.Split(':');
            var entityType = parts[0];
            
            var versionKey = $"version:{entityType}";
            if (!_cache.TryGetValue(versionKey, out string? version) || string.IsNullOrEmpty(version))
            {
                version = Guid.NewGuid().ToString("N");
                _cache.Set(versionKey, version, TimeSpan.FromHours(1));
            }

            return $"{baseKey}:{version}";
        }

        public void BumpVersion(string entityType)
        {
            var versionKey = $"version:{entityType}";
            var newVersion = Guid.NewGuid().ToString("N");
            _cache.Set(versionKey, newVersion, TimeSpan.FromHours(1));
            _logger.LogInformation("Cache version bumped for {EntityType} -> {NewVersion}", entityType, newVersion);
        }
    }
}
