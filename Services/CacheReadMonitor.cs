using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace API_DigiBook.Services
{
    public static class CacheReadMonitor
    {
        private static readonly ConcurrentDictionary<string, int> Counters = new();

        public static void Record(string key, ILogger logger)
        {
            var count = Counters.AddOrUpdate(key, 1, (_, current) => current + 1);

            if (count % 50 == 0)
            {
                logger.LogInformation("Cache read monitor: {Key} miss count = {Count}", key, count);
            }
        }
    }
}
