using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Notifications.Channels;
using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Notifications.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;

namespace API_DigiBook.Notifications.Observers
{
    public class TelegramNotificationObserver : INotificationObserver
    {
        private readonly TelegramNotificationChannel _channel;
        private readonly INotificationLogRepository _logRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationOptions _options;
        private readonly ILogger<TelegramNotificationObserver> _logger;

        public string ObserverName => "TelegramNotificationObserver";

        public TelegramNotificationObserver(
            TelegramNotificationChannel channel,
            INotificationLogRepository logRepository,
            IUserRepository userRepository,
            IOptions<NotificationOptions> options,
            ILogger<TelegramNotificationObserver> logger)
        {
            _channel = channel;
            _logRepository = logRepository;
            _userRepository = userRepository;
            _options = options.Value;
            _logger = logger;
        }

        public bool CanHandle(string eventType)
        {
            return eventType == NotificationEventTypes.OrderCreated ||
                   eventType == NotificationEventTypes.PaymentPaid ||
                   eventType == NotificationEventTypes.OrderStatusChanged;
        }

        public async Task HandleAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableTelegram)
            {
                return;
            }

            var chatId = await ResolveChatIdAsync(notificationEvent.UserId);
            if (string.IsNullOrWhiteSpace(chatId))
            {
                await SaveLogAsync(BuildBaseLog(notificationEvent, "telegram", "Skipped", "", "User has no telegramChatId."));
                return;
            }

            var idempotencyKey = BuildIdempotencyKey(notificationEvent, "telegram", chatId);
            if (await _logRepository.HasSentAsync(idempotencyKey))
            {
                await SaveLogAsync(BuildBaseLog(notificationEvent, "telegram", "Skipped", chatId, "Already sent by idempotency key.", idempotencyKey));
                return;
            }

            var pendingLog = BuildBaseLog(notificationEvent, "telegram", "Pending", chatId, "", idempotencyKey);
            await SaveLogAsync(pendingLog);

            var message = BuildTelegramMessage(notificationEvent);
            var maxAttempt = Math.Max(1, _options.RetryCount + 1);
            NotificationChannelResult? sendResult = null;

            for (var attempt = 1; attempt <= maxAttempt; attempt++)
            {
                pendingLog.Attempt = attempt;
                pendingLog.UpdatedAt = Timestamp.GetCurrentTimestamp();
                await _logRepository.UpdateAsync(pendingLog.Id, pendingLog);

                sendResult = await _channel.SendAsync(chatId, message, cancellationToken);
                if (sendResult.Success)
                {
                    break;
                }

                if (attempt < maxAttempt)
                {
                    await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);
                }
            }

            if (sendResult != null && sendResult.Success)
            {
                pendingLog.Status = "Sent";
                pendingLog.ProviderResponse = sendResult.ProviderResponse;
                pendingLog.ErrorMessage = string.Empty;
                pendingLog.SentAt = Timestamp.GetCurrentTimestamp();
            }
            else
            {
                pendingLog.Status = "Failed";
                pendingLog.ProviderResponse = sendResult?.ProviderResponse ?? string.Empty;
                pendingLog.ErrorMessage = sendResult?.ErrorMessage ?? "Unknown Telegram error";
            }

            pendingLog.UpdatedAt = Timestamp.GetCurrentTimestamp();
            await _logRepository.UpdateAsync(pendingLog.Id, pendingLog);

            _logger.LogInformation(
                "Telegram observer completed EventType={EventType}, EventId={EventId}, Status={Status}",
                notificationEvent.EventType,
                notificationEvent.EventId,
                pendingLog.Status);
        }

        private async Task<string> ResolveChatIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return string.Empty;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            return user?.TelegramChatId?.Trim() ?? string.Empty;
        }

        private static string BuildTelegramMessage(NotificationEvent notificationEvent)
        {
            var orderId = Safe(notificationEvent.OrderId, "N/A");
            var status = Safe(notificationEvent.NewStatus, "Dang xu ly");
            var oldStatus = Safe(notificationEvent.OldStatus, "N/A");
            var customerName = Safe(notificationEvent.CustomerName, "Khach hang");
            var paymentMethod = Safe(notificationEvent.PaymentMethod, "N/A");
            var paymentStatus = Safe(notificationEvent.PaymentStatus, "N/A");
            var itemSummary = Safe(notificationEvent.ItemSummary, "Khong co san pham");
            var customerAddress = Safe(notificationEvent.CustomerAddress, "N/A");

            var headline = notificationEvent.EventType switch
            {
                NotificationEventTypes.OrderCreated => "Thong bao don hang moi",
                NotificationEventTypes.PaymentPaid => "Thong bao thanh toan thanh cong",
                NotificationEventTypes.OrderStatusChanged => "Thong bao cap nhat trang thai",
                _ => "Thong bao DigiBook"
            };

            var statusLine = notificationEvent.EventType switch
            {
                NotificationEventTypes.OrderStatusChanged => $"<b>Trang thai:</b> {oldStatus} -> {status}",
                _ => $"<b>Trang thai:</b> {status}"
            };

            return notificationEvent.EventType switch
            {
                NotificationEventTypes.OrderCreated or NotificationEventTypes.PaymentPaid or NotificationEventTypes.OrderStatusChanged =>
                    $"<b>DigiBook | {headline}</b>\n" +
                    $"<b>Ma don:</b> <code>{orderId}</code>\n" +
                    $"{statusLine}\n" +
                    $"<b>Khach hang:</b> {customerName}\n" +
                    $"<b>Thanh toan:</b> {paymentMethod} ({paymentStatus})\n" +
                    $"<b>So san pham:</b> {notificationEvent.ItemCount}\n" +
                    $"<b>Tong tien:</b> {FormatMoney(notificationEvent.Total)}\n" +
                    $"<b>San pham:</b> {itemSummary}\n" +
                    $"<b>Dia chi:</b> {customerAddress}",
                _ => "<b>DigiBook</b>\nBan co thong bao moi."
            };
        }

        private static string FormatMoney(double amount)
        {
            return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} VND", amount);
        }

        private static string Safe(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : WebUtility.HtmlEncode(value.Trim());
        }

        private static string BuildIdempotencyKey(NotificationEvent notificationEvent, string channel, string recipient)
        {
            return $"{notificationEvent.EventType}:{notificationEvent.EventId}:{channel}:{recipient}".ToLowerInvariant();
        }

        private static NotificationLog BuildBaseLog(
            NotificationEvent notificationEvent,
            string channel,
            string status,
            string recipient,
            string errorMessage,
            string idempotencyKey = "")
        {
            return new NotificationLog
            {
                Id = Guid.NewGuid().ToString("N"),
                EventId = notificationEvent.EventId,
                EventType = notificationEvent.EventType,
                Channel = channel,
                Recipient = recipient,
                Status = status,
                IdempotencyKey = idempotencyKey,
                ErrorMessage = errorMessage,
                CreatedAt = Timestamp.GetCurrentTimestamp(),
                UpdatedAt = Timestamp.GetCurrentTimestamp()
            };
        }

        private async Task SaveLogAsync(NotificationLog log)
        {
            await _logRepository.AddAsync(log, log.Id);
        }
    }
}
