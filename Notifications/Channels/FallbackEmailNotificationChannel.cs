using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Notifications.Channels
{
    public class FallbackEmailNotificationChannel : IEmailNotificationChannel
    {
        private readonly GmailApiEmailNotificationChannel _gmailApiChannel;
        private readonly ResendEmailNotificationChannel _resendChannel;
        private readonly SmtpEmailNotificationChannel _smtpChannel;
        private readonly NotificationOptions _options;
        private readonly GmailApiOptions _gmailApiOptions;
        private readonly ILogger<FallbackEmailNotificationChannel> _logger;

        public FallbackEmailNotificationChannel(
            GmailApiEmailNotificationChannel gmailApiChannel,
            ResendEmailNotificationChannel resendChannel,
            SmtpEmailNotificationChannel smtpChannel,
            IOptions<NotificationOptions> options,
            IOptions<GmailApiOptions> gmailApiOptions,
            ILogger<FallbackEmailNotificationChannel> logger)
        {
            _gmailApiChannel = gmailApiChannel;
            _resendChannel = resendChannel;
            _smtpChannel = smtpChannel;
            _options = options.Value;
            _gmailApiOptions = gmailApiOptions.Value;
            _logger = logger;
        }

        public async Task<NotificationChannelResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            var preferredProvider = (_options.Email.Provider ?? string.Empty).Trim().ToLowerInvariant();
            var gmailApiConfigured = GmailApiEmailNotificationChannel.IsConfigured(_gmailApiOptions);
            var resendConfigured = ResendEmailNotificationChannel.IsConfigured(_options);

            var primary = preferredProvider switch
            {
                "gmailapi" => "gmailapi",
                "gmail" => "gmailapi",
                "smtp" => "smtp",
                _ => "resend"
            };

            if (primary == "gmailapi" && !gmailApiConfigured)
            {
                primary = "smtp";
            }

            if (primary == "resend" && !resendConfigured)
            {
                primary = "smtp";
            }

            if (primary == "gmailapi")
            {
                var gmailApiResult = await _gmailApiChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (gmailApiResult.Success)
                {
                    return gmailApiResult;
                }

                _logger.LogWarning("Gmail API failed, fallback to SMTP. Error={Error}", gmailApiResult.ErrorMessage);
                var smtpFallback = await _smtpChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (smtpFallback.Success)
                {
                    return NotificationChannelResult.Ok($"Fallback SMTP accepted after Gmail API failure: {gmailApiResult.ErrorMessage}");
                }

                if (!resendConfigured)
                {
                    return NotificationChannelResult.Fail(
                        $"Gmail API failed: {gmailApiResult.ErrorMessage} | SMTP fallback failed: {smtpFallback.ErrorMessage}");
                }

                _logger.LogWarning("SMTP fallback failed after Gmail API, trying Resend. Error={Error}", smtpFallback.ErrorMessage);
                var resendFallback = await _resendChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (resendFallback.Success)
                {
                    return NotificationChannelResult.Ok(
                        $"Fallback Resend accepted after Gmail API and SMTP failures: {gmailApiResult.ErrorMessage} | {smtpFallback.ErrorMessage}");
                }

                return NotificationChannelResult.Fail(
                    $"Gmail API failed: {gmailApiResult.ErrorMessage} | SMTP fallback failed: {smtpFallback.ErrorMessage} | Resend fallback failed: {resendFallback.ErrorMessage}");
            }

            if (primary == "resend")
            {
                var resendResult = await _resendChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (resendResult.Success)
                {
                    return resendResult;
                }

                _logger.LogWarning("Resend failed, fallback to SMTP. Error={Error}", resendResult.ErrorMessage);
                var smtpResult = await _smtpChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (smtpResult.Success)
                {
                    return NotificationChannelResult.Ok($"Fallback SMTP accepted after Resend failure: {resendResult.ErrorMessage}");
                }

                return NotificationChannelResult.Fail(
                    $"Resend failed: {resendResult.ErrorMessage} | SMTP fallback failed: {smtpResult.ErrorMessage}");
            }

            var primarySmtp = await _smtpChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
            if (primarySmtp.Success)
            {
                return primarySmtp;
            }

            if (gmailApiConfigured)
            {
                _logger.LogWarning("SMTP failed, fallback to Gmail API. Error={Error}", primarySmtp.ErrorMessage);
                var gmailApiFallback = await _gmailApiChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (gmailApiFallback.Success)
                {
                    return NotificationChannelResult.Ok($"Fallback Gmail API accepted after SMTP failure: {primarySmtp.ErrorMessage}");
                }

                if (!resendConfigured)
                {
                    return NotificationChannelResult.Fail(
                        $"SMTP failed: {primarySmtp.ErrorMessage} | Gmail API fallback failed: {gmailApiFallback.ErrorMessage}");
                }

                _logger.LogWarning("Gmail API fallback failed after SMTP, trying Resend. Error={Error}", gmailApiFallback.ErrorMessage);
                var resendThirdFallback = await _resendChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (resendThirdFallback.Success)
                {
                    return NotificationChannelResult.Ok(
                        $"Fallback Resend accepted after SMTP and Gmail API failures: {primarySmtp.ErrorMessage} | {gmailApiFallback.ErrorMessage}");
                }

                return NotificationChannelResult.Fail(
                    $"SMTP failed: {primarySmtp.ErrorMessage} | Gmail API fallback failed: {gmailApiFallback.ErrorMessage} | Resend fallback failed: {resendThirdFallback.ErrorMessage}");
            }

            if (!resendConfigured)
            {
                return primarySmtp;
            }

            _logger.LogWarning("SMTP failed, fallback to Resend. Error={Error}", primarySmtp.ErrorMessage);
            var resendAfterSmtp = await _resendChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
            if (resendAfterSmtp.Success)
            {
                return NotificationChannelResult.Ok($"Fallback Resend accepted after SMTP failure: {primarySmtp.ErrorMessage}");
            }

            return NotificationChannelResult.Fail(
                $"SMTP failed: {primarySmtp.ErrorMessage} | Resend fallback failed: {resendAfterSmtp.ErrorMessage}");
        }
    }
}
