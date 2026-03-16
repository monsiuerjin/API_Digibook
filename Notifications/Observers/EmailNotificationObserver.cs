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
    public class EmailNotificationObserver : INotificationObserver
    {
        private readonly IEmailNotificationChannel _channel;
        private readonly INotificationLogRepository _logRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationOptions _options;
        private readonly ILogger<EmailNotificationObserver> _logger;

        public string ObserverName => "EmailNotificationObserver";

        public EmailNotificationObserver(
            IEmailNotificationChannel channel,
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
                        var customerName = Safe(notificationEvent.CustomerName, "Quy khach");
                        var orderId = Safe(notificationEvent.OrderId, "N/A");
                        var status = Safe(notificationEvent.NewStatus, "Dang xu ly");
                        var oldStatus = Safe(notificationEvent.OldStatus, "N/A");
                        var paymentMethod = Safe(notificationEvent.PaymentMethod, "N/A");
                        var paymentProvider = Safe(notificationEvent.PaymentProvider, "N/A");
                        var paymentStatus = Safe(notificationEvent.PaymentStatus, "N/A");
                        var transactionId = Safe(notificationEvent.TransactionId, "N/A");
                        var customerPhone = Safe(notificationEvent.CustomerPhone, "N/A");
                        var customerEmail = Safe(notificationEvent.CustomerEmail, "N/A");
                        var customerAddress = Safe(notificationEvent.CustomerAddress, "N/A");
                        var orderDate = Safe(notificationEvent.OrderDate, "N/A");
                        var itemSummary = Safe(notificationEvent.ItemSummary, "Khong co san pham");

                        var emailTitle = notificationEvent.EventType switch
                        {
                                NotificationEventTypes.OrderCreated => "Xac nhan don hang",
                                NotificationEventTypes.PaymentPaid => "Xac nhan thanh toan",
                                NotificationEventTypes.OrderStatusChanged => "Cap nhat trang thai don hang",
                                _ => "Thong bao DigiBook"
                        };

                        var statusNote = notificationEvent.EventType switch
                        {
                                NotificationEventTypes.OrderCreated => "Don hang cua ban da duoc tiep nhan thanh cong va dang duoc xu ly.",
                                NotificationEventTypes.PaymentPaid => "Thanh toan cua ban da duoc ghi nhan thanh cong.",
                                NotificationEventTypes.OrderStatusChanged => $"Trang thai don hang da thay doi tu {oldStatus} sang {status}.",
                                _ => "Ban co mot thong bao moi tu he thong DigiBook."
                        };

                        var body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background:#f4f6fb;font-family:Segoe UI,Arial,sans-serif;color:#1f2937;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background:#f4f6fb;padding:24px 12px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='640' cellspacing='0' cellpadding='0' style='max-width:640px;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e5e7eb;'>
                    <tr>
                        <td style='background:#0f172a;color:#ffffff;padding:20px 24px;'>
                            <div style='font-size:22px;font-weight:700;letter-spacing:0.2px;'>DigiBook</div>
                            <div style='margin-top:6px;font-size:14px;opacity:.9;'>Thong bao don hang</div>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:24px;'>
                            <h2 style='margin:0 0 8px;font-size:20px;color:#0f172a;'>{emailTitle}</h2>
                            <p style='margin:0 0 18px;line-height:1.6;'>Xin chao <b>{customerName}</b>, {statusNote}</p>

                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border-collapse:collapse;margin-bottom:16px;'>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Ma don</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{orderId}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Ngay dat</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{orderDate}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Trang thai</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{status}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Thanh toan</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{paymentMethod} ({paymentProvider}) - {paymentStatus}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Ma giao dich</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{transactionId}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>So luong san pham</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{notificationEvent.ItemCount}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Tam tinh</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{FormatMoney(notificationEvent.Subtotal)}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Phi van chuyen</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>{FormatMoney(notificationEvent.Shipping)}</td></tr>
                                <tr><td style='padding:8px 0;border-bottom:1px solid #eef2f7;color:#64748b;'>Giam gia coupon</td><td style='padding:8px 0;border-bottom:1px solid #eef2f7;text-align:right;font-weight:600;'>-{FormatMoney(notificationEvent.CouponDiscount)}</td></tr>
                                <tr><td style='padding:12px 0 0;color:#0f172a;font-weight:700;'>Tong thanh toan</td><td style='padding:12px 0 0;text-align:right;color:#0f172a;font-weight:700;font-size:18px;'>{FormatMoney(notificationEvent.Total)}</td></tr>
                            </table>

                            <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:12px;margin-bottom:14px;'>
                                <div style='font-size:13px;color:#64748b;margin-bottom:6px;'>San pham</div>
                                <div style='font-size:14px;line-height:1.6;'>{itemSummary}</div>
                            </div>

                            <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:12px;'>
                                <div style='font-size:13px;color:#64748b;margin-bottom:6px;'>Thong tin nguoi nhan</div>
                                <div style='font-size:14px;line-height:1.6;'>
                                    {customerName}<br/>
                                    {customerPhone}<br/>
                                    {customerEmail}<br/>
                                    {customerAddress}
                                </div>
                            </div>

                            <p style='margin:18px 0 0;font-size:13px;color:#64748b;line-height:1.6;'>Neu ban can ho tro, vui long lien he DigiBook de duoc ho tro nhanh nhat.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            return notificationEvent.EventType switch
            {
                NotificationEventTypes.OrderCreated => (
                                        $"[DigiBook] Xac nhan don hang {orderId}",
                                        body
                ),
                NotificationEventTypes.PaymentPaid => (
                                        $"[DigiBook] Thanh toan thanh cong - Don {orderId}",
                                        body
                ),
                NotificationEventTypes.OrderStatusChanged => (
                                        $"[DigiBook] Cap nhat trang thai - Don {orderId}",
                                        body
                ),
                _ => (
                    "[DigiBook] Notification",
                                        body
                )
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
