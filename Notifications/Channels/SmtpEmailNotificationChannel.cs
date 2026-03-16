using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace API_DigiBook.Notifications.Channels
{
    public class SmtpEmailNotificationChannel
    {
        private readonly NotificationOptions _options;

        public SmtpEmailNotificationChannel(IOptions<NotificationOptions> options)
        {
            _options = options.Value;
        }

        public async Task<NotificationChannelResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return NotificationChannelResult.Fail("Recipient email is empty.");
            }

            if (string.IsNullOrWhiteSpace(_options.Email.Host) ||
                string.IsNullOrWhiteSpace(_options.Email.Username) ||
                string.IsNullOrWhiteSpace(_options.Email.AppPassword) ||
                string.IsNullOrWhiteSpace(_options.Email.FromEmail))
            {
                return NotificationChannelResult.Fail("Email SMTP configuration is incomplete.");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.Email.FromName, _options.Email.FromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var smtp = new SmtpClient();
                smtp.Timeout = _options.Email.TimeoutMilliseconds;

                var secureSocketOptions = _options.Email.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await smtp.ConnectAsync(
                    _options.Email.Host,
                    _options.Email.Port,
                    secureSocketOptions,
                    cancellationToken);

                await smtp.AuthenticateAsync(
                    _options.Email.Username,
                    _options.Email.AppPassword,
                    cancellationToken);

                await smtp.SendAsync(message, cancellationToken);
                await smtp.DisconnectAsync(true, cancellationToken);

                return NotificationChannelResult.Ok("SMTP accepted");
            }
            catch (Exception ex)
            {
                return NotificationChannelResult.Fail(ex.Message);
            }
        }
    }
}
