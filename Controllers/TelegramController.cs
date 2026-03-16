using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Models;
using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly FirestoreDb _db;
        private readonly NotificationOptions _notificationOptions;
        private readonly ILogger<TelegramController> _logger;

        public TelegramController(
            IUserRepository userRepository,
            IOptions<NotificationOptions> notificationOptions,
            ILogger<TelegramController> logger)
        {
            _userRepository = userRepository;
            _db = FirebaseService.GetFirestoreDb();
            _notificationOptions = notificationOptions.Value;
            _logger = logger;
        }

        [HttpPost("link-token")]
        public async Task<IActionResult> CreateLinkToken([FromBody] CreateTelegramLinkTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { message = "UserId is required" });
            }

            if (string.IsNullOrWhiteSpace(_notificationOptions.Telegram.BotUsername))
            {
                return BadRequest(new { message = "Telegram BotUsername is not configured" });
            }

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var token = Guid.NewGuid().ToString("N");
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(10);

            var linkToken = new TelegramLinkToken
            {
                Id = token,
                Token = token,
                UserId = request.UserId,
                IsUsed = false,
                ExpiresAt = Timestamp.FromDateTime(expiresAtUtc),
                CreatedAt = Timestamp.GetCurrentTimestamp()
            };

            await _db.Collection("telegramLinkTokens").Document(token).SetAsync(linkToken);

            var startLink = $"https://t.me/{_notificationOptions.Telegram.BotUsername}?start={token}";
            return Ok(new
            {
                token,
                startLink,
                expiresAtUtc
            });
        }

        [HttpGet("link-status/{userId}")]
        public async Task<IActionResult> GetLinkStatus(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var now = Timestamp.FromDateTime(DateTime.UtcNow);
            var pendingQuery = _db.Collection("telegramLinkTokens")
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("isUsed", false)
                .WhereGreaterThan("expiresAt", now)
                .OrderByDescending("createdAt")
                .Limit(1);

            var pendingSnapshot = await pendingQuery.GetSnapshotAsync();
            var pendingToken = pendingSnapshot.Documents.FirstOrDefault()?.ConvertTo<TelegramLinkToken>();

            return Ok(new
            {
                isLinked = !string.IsNullOrWhiteSpace(user.TelegramChatId),
                telegramChatId = user.TelegramChatId,
                hasPendingToken = pendingToken != null,
                pendingTokenExpiresAt = pendingToken?.ExpiresAt.ToDateTime()
            });
        }

        [HttpDelete("unlink/{userId}")]
        public async Task<IActionResult> Unlink(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.TelegramChatId = string.Empty;
            user.UpdatedAt = Timestamp.GetCurrentTimestamp();
            await _userRepository.UpdateAsync(userId, user);

            return Ok(new { message = "Telegram unlinked successfully" });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] JsonElement update)
        {
            try
            {
                if (!TryExtractStartPayload(update, out var token, out var chatId))
                {
                    return Ok(new { message = "No link payload found" });
                }

                var tokenDoc = await _db.Collection("telegramLinkTokens").Document(token).GetSnapshotAsync();
                if (!tokenDoc.Exists)
                {
                    return Ok(new { message = "Invalid token" });
                }

                var linkToken = tokenDoc.ConvertTo<TelegramLinkToken>();
                if (linkToken.IsUsed)
                {
                    return Ok(new { message = "Token already used" });
                }

                if (linkToken.ExpiresAt.ToDateTime() <= DateTime.UtcNow)
                {
                    return Ok(new { message = "Token expired" });
                }

                var user = await _userRepository.GetByIdAsync(linkToken.UserId);
                if (user == null)
                {
                    return Ok(new { message = "User not found" });
                }

                user.TelegramChatId = chatId;
                user.UpdatedAt = Timestamp.GetCurrentTimestamp();
                await _userRepository.UpdateAsync(user.Id, user);

                linkToken.IsUsed = true;
                linkToken.UsedAt = Timestamp.GetCurrentTimestamp();
                await _db.Collection("telegramLinkTokens").Document(linkToken.Id).SetAsync(linkToken, SetOptions.MergeAll);

                return Ok(new { message = "Telegram linked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Telegram webhook");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private static bool TryExtractStartPayload(JsonElement update, out string token, out string chatId)
        {
            token = string.Empty;
            chatId = string.Empty;

            if (!update.TryGetProperty("message", out var message))
            {
                return false;
            }

            if (!message.TryGetProperty("text", out var textElement))
            {
                return false;
            }

            var text = textElement.GetString() ?? string.Empty;
            if (!text.StartsWith("/start ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!message.TryGetProperty("chat", out var chatElement) ||
                !chatElement.TryGetProperty("id", out var chatIdElement))
            {
                return false;
            }

            token = text.Replace("/start ", string.Empty).Trim();
            chatId = chatIdElement.ValueKind == JsonValueKind.Number
                ? chatIdElement.GetInt64().ToString()
                : (chatIdElement.GetString() ?? string.Empty);

            return !string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(chatId);
        }
    }

    public class CreateTelegramLinkTokenRequest
    {
        public string UserId { get; set; } = string.Empty;
    }
}
