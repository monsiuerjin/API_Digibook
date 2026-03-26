using Microsoft.AspNetCore.Mvc;
using API_DigiBook.Singleton;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly LoggerService _logger;

        public LogsController()
        {
            // Get singleton instance
            _logger = LoggerService.Instance;
        }

        /// <summary>
        /// Get all system logs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            try
            {
                var logs = await _logger.GetAllLogsAsync();

                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving logs",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get logs by status (SUCCESS, ERROR, WARNING, INFO)
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetLogsByStatus(string status)
        {
            try
            {
                var logs = await _logger.GetLogsByStatusAsync(status.ToUpper());

                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving logs",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get logs by user
        /// </summary>
        [HttpGet("user/{user}")]
        public async Task<IActionResult> GetLogsByUser(string user)
        {
            try
            {
                var logs = await _logger.GetLogsByUserAsync(user);

                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving logs",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get logs by action
        /// </summary>
        [HttpGet("action/{action}")]
        public async Task<IActionResult> GetLogsByAction(string action)
        {
            try
            {
                var logs = await _logger.GetLogsByActionAsync(action);

                return Ok(new
                {
                    success = true,
                    count = logs.Count,
                    data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving logs",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _logger.GetLogStatisticsAsync();

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving statistics",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a test log (for testing purposes)
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> CreateTestLog([FromQuery] string action = "TEST", [FromQuery] string detail = "Test log entry")
        {
            try
            {
                var logId = await _logger.LogInfoAsync(action, detail, "TestUser");

                return Ok(new
                {
                    success = true,
                    message = "Test log created",
                    logId = logId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating test log",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete old logs (older than specified days)
        /// </summary>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupOldLogs([FromQuery] int? days = null, [FromQuery] string? beforeDate = null)
        {
            try
            {
                int daysToUse = days ?? 30;
                if (!string.IsNullOrWhiteSpace(beforeDate))
                {
                    if (!DateTime.TryParse(beforeDate, out var parsedDate))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid beforeDate format"
                        });
                    }

                    var diff = DateTime.UtcNow.Date - parsedDate.ToUniversalTime().Date;
                    daysToUse = Math.Max(0, (int)Math.Ceiling(diff.TotalDays));
                }

                var deletedCount = await _logger.DeleteOldLogsAsync(daysToUse);

                return Ok(new
                {
                    success = true,
                    message = $"Deleted {deletedCount} old logs",
                    data = new { deletedCount = deletedCount },
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting old logs",
                    error = ex.Message
                });
            }
        }
    }
}
