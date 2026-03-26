using API_DigiBook.Models;
using API_DigiBook.Services;
using Google.Cloud.Firestore;

namespace API_DigiBook.Singleton
{
    /// <summary>
    /// Singleton Logger Service for logging system activities to Firestore
    /// </summary>
    public sealed class LoggerService
    {
        private static LoggerService? _instance;
        private static readonly object _lock = new object();
        private readonly FirestoreDb _db;
        private readonly string _collectionName = "system_logs";

        // Private constructor to prevent instantiation
        private LoggerService()
        {
            _db = FirebaseService.GetFirestoreDb();
        }

        /// <summary>
        /// Get the singleton instance of LoggerService
        /// </summary>
        public static LoggerService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoggerService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Log an action to Firestore
        /// </summary>
        public async Task<string> LogAsync(
            string action, 
            string detail, 
            string status = "SUCCESS", 
            string user = "Anonymous")
        {
            try
            {
                var log = new SystemLog
                {
                    Id = Guid.NewGuid().ToString(),
                    Action = action,
                    Detail = detail,
                    Status = status,
                    User = user,
                    CreatedAt = Timestamp.GetCurrentTimestamp()
                };

                var docRef = _db.Collection(_collectionName).Document(log.Id);
                await docRef.SetAsync(log);

                Console.WriteLine($"[LOG] {status} - {action}: {detail}");
                return log.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to log: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Log a success action
        /// </summary>
        public async Task<string> LogSuccessAsync(string action, string detail, string user = "Anonymous")
        {
            return await LogAsync(action, detail, "SUCCESS", user);
        }

        /// <summary>
        /// Log an error action
        /// </summary>
        public async Task<string> LogErrorAsync(string action, string detail, string user = "Anonymous")
        {
            return await LogAsync(action, detail, "ERROR", user);
        }

        /// <summary>
        /// Log a warning action
        /// </summary>
        public async Task<string> LogWarningAsync(string action, string detail, string user = "Anonymous")
        {
            return await LogAsync(action, detail, "WARNING", user);
        }

        /// <summary>
        /// Log an info action
        /// </summary>
        public async Task<string> LogInfoAsync(string action, string detail, string user = "Anonymous")
        {
            return await LogAsync(action, detail, "INFO", user);
        }

        /// <summary>
        /// Get all logs
        /// </summary>
        public async Task<List<SystemLog>> GetAllLogsAsync()
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .OrderByDescending("createdAt")
                    .Limit(1000);

                var snapshot = await query.GetSnapshotAsync();
                var logs = new List<SystemLog>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var log = document.ConvertTo<SystemLog>();
                        log.Id = document.Id;
                        logs.Add(log);
                    }
                }

                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get logs: {ex.Message}");
                return new List<SystemLog>();
            }
        }

        /// <summary>
        /// Get logs by status
        /// </summary>
        public async Task<List<SystemLog>> GetLogsByStatusAsync(string status)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("status", status)
                    .OrderByDescending("createdAt")
                    .Limit(500);

                var snapshot = await query.GetSnapshotAsync();
                var logs = new List<SystemLog>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var log = document.ConvertTo<SystemLog>();
                        log.Id = document.Id;
                        logs.Add(log);
                    }
                }

                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get logs by status: {ex.Message}");
                return new List<SystemLog>();
            }
        }

        /// <summary>
        /// Get logs by user
        /// </summary>
        public async Task<List<SystemLog>> GetLogsByUserAsync(string user)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("user", user)
                    .OrderByDescending("createdAt")
                    .Limit(500);

                var snapshot = await query.GetSnapshotAsync();
                var logs = new List<SystemLog>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var log = document.ConvertTo<SystemLog>();
                        log.Id = document.Id;
                        logs.Add(log);
                    }
                }

                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get logs by user: {ex.Message}");
                return new List<SystemLog>();
            }
        }

        /// <summary>
        /// Get logs by action
        /// </summary>
        public async Task<List<SystemLog>> GetLogsByActionAsync(string action)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("action", action)
                    .OrderByDescending("createdAt")
                    .Limit(500);

                var snapshot = await query.GetSnapshotAsync();
                var logs = new List<SystemLog>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var log = document.ConvertTo<SystemLog>();
                        log.Id = document.Id;
                        logs.Add(log);
                    }
                }

                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get logs by action: {ex.Message}");
                return new List<SystemLog>();
            }
        }

        /// <summary>
        /// Delete old logs (older than specified days)
        /// </summary>
        public async Task<int> DeleteOldLogsAsync(int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                var cutoffTimestamp = Timestamp.FromDateTime(cutoffDate);

                var query = _db.Collection(_collectionName)
                    .WhereLessThan("createdAt", cutoffTimestamp);

                var snapshot = await query.GetSnapshotAsync();
                int deletedCount = 0;

                foreach (var document in snapshot.Documents)
                {
                    await document.Reference.DeleteAsync();
                    deletedCount++;
                }

                Console.WriteLine($"[LOG] Deleted {deletedCount} old logs");
                return deletedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete old logs: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetLogStatisticsAsync()
        {
            try
            {
                var logs = await GetAllLogsAsync();
                
                var stats = new Dictionary<string, int>
                {
                    { "Total", logs.Count },
                    { "SUCCESS", logs.Count(l => l.Status == "SUCCESS") },
                    { "ERROR", logs.Count(l => l.Status == "ERROR") },
                    { "WARNING", logs.Count(l => l.Status == "WARNING") },
                    { "INFO", logs.Count(l => l.Status == "INFO") }
                };

                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get log statistics: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }
    }
}
