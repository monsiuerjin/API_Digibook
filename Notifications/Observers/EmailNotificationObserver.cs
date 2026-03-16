using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Notifications.Channels;
using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Notifications.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Notifications.Observers
{
    public class EmailNotificationObserver : INotificationObserver
    {
        private readonly SmtpEmailNotificationChannel _channel;
        private readonly INotificationLogRepository _logRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationOptions _options;
        private readonly ILogger<EmailNotificationObserver> _logger;

        public string ObserverName => "EmailNotificationObserver";

        public EmailNotificationObserver(
            SmtpEmailNotificationChannel channel,
            INotificationLogRepository logRepository,
            IUserRepository userRepository,
            IOptions<NotificationOptions> options,
            ILogger<EmailNotificationObserver> logger)
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
            if (!_options.EnableEmail)
            {
                return;
            }

            var recipient = await ResolveRecipientEmailAsync(notificationEvent.UserId, notificationEvent.CustomerEmail);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                await SaveLogAsync(BuildBaseLog(notificationEvent, "email", "Skipped", "", "No recipient email resolved."));
                return;
            }

            var idempotencyKey = BuildIdempotencyKey(notificationEvent, "email", recipient);
            if (await _logRepository.HasSentAsync(idempotencyKey))
            {
                await SaveLogAsync(BuildBaseLog(notificationEvent, "email", "Skipped", recipient, "Already sent by idempotency key.", idempotencyKey));
                return;
            }

            var (subject, body) = BuildEmailTemplate(notificationEvent);
            var pendingLog = BuildBaseLog(notificationEvent, "email", "Pending", recipient, "", idempotencyKey);
            await SaveLogAsync(pendingLog);

            var maxAttempt = Math.Max(1, _options.RetryCount + 1);
            NotificationChannelResult? sendResult = null;

            for (var attempt = 1; attempt <= maxAttempt; attempt++)
            {
                pendingLog.Attempt = attempt;
                pendingLog.UpdatedAt = Timestamp.GetCurrentTimestamp();
                await _logRepository.UpdateAsync(pendingLog.Id, pendingLog);

                sendResult = await _channel.SendAsync(recipient, subject, body, cancellationToken);
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
                pendingLog.ErrorMessage = sendResult?.ErrorMessage ?? "Unknown SMTP error";
            }

            pendingLog.UpdatedAt = Timestamp.GetCurrentTimestamp();
            await _logRepository.UpdateAsync(pendingLog.Id, pendingLog);

            _logger.LogInformation(
                "Email observer completed EventType={EventType}, EventId={EventId}, Status={Status}",
                notificationEvent.EventType,
                notificationEvent.EventId,
                pendingLog.Status);
        }

        private async Task<string> ResolveRecipientEmailAsync(string userId, string customerEmail)
        {
            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                return customerEmail.Trim();
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return string.Empty;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            return user?.Email?.Trim() ?? string.Empty;
        }

        private static string BuildIdempotencyKey(NotificationEvent notificationEvent, string channel, string recipient)
        {
            return $"{notificationEvent.EventType}:{notificationEvent.EventId}:{channel}:{recipient}".ToLowerInvariant();
        }

        private static (string Subject, string Body) BuildEmailTemplate(NotificationEvent notificationEvent)
        {
            return notificationEvent.EventType switch
            {
                NotificationEventTypes.OrderCreated => (
                    $"[DigiBook] Don hang {notificationEvent.OrderId} da duoc tao",
                    $"<p>Chao ban,</p><p>Don hang <b>{notificationEvent.OrderId}</b> da duoc tao thanh cong.</p><p>Trang thai hien tai: <b>{notificationEvent.NewStatus ?? "Dang xu ly"}</b>.</p>"
                ),
                NotificationEventTypes.PaymentPaid => (
                    $"[DigiBook] Thanh toan thanh cong cho don {notificationEvent.OrderId}",
                    $"<p>Chao ban,</p><p>Don hang <b>{notificationEvent.OrderId}</b> da thanh toan thanh cong.</p><p>Cam on ban da mua hang tai DigiBook.</p>"
                ),
                NotificationEventTypes.OrderStatusChanged => (
                    $"[DigiBook] Cap nhat trang thai don {notificationEvent.OrderId}",
                    $"<p>Chao ban,</p><p>Don hang <b>{notificationEvent.OrderId}</b> da cap nhat trang thai tu <b>{notificationEvent.OldStatus ?? "N/A"}</b> sang <b>{notificationEvent.NewStatus ?? "N/A"}</b>.</p>"
                ),
                _ => (
                    "[DigiBook] Notification",
                    "<p>Ban co thong bao moi tu DigiBook.</p>"
                )
            };
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
