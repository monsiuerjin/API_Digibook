using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace API_DigiBook.Notifications.Channels
{
    public class GmailApiEmailNotificationChannel : IEmailNotificationChannel
    {
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string GmailSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

        private readonly HttpClient _httpClient;
        private readonly GmailApiOptions _gmailApiOptions;
        private readonly NotificationOptions _notificationOptions;
        private readonly ILogger<GmailApiEmailNotificationChannel> _logger;

        public GmailApiEmailNotificationChannel(
            HttpClient httpClient,
            IOptions<GmailApiOptions> gmailApiOptions,
            IOptions<NotificationOptions> notificationOptions,
            ILogger<GmailApiEmailNotificationChannel> logger)
        {
            _httpClient = httpClient;
            _gmailApiOptions = gmailApiOptions.Value;
            _notificationOptions = notificationOptions.Value;
            _logger = logger;
        }

        public async Task<NotificationChannelResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            var recipient = toEmail?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                return NotificationChannelResult.Fail("Recipient email is empty.");
            }

            if (!IsConfigured(_gmailApiOptions))
            {
                return NotificationChannelResult.Fail("Gmail API configuration is incomplete.");
            }

            try
            {
                var accessTokenResult = await ExchangeRefreshTokenAsync(cancellationToken);
                if (!accessTokenResult.Success || string.IsNullOrWhiteSpace(accessTokenResult.AccessToken))
                {
                    return NotificationChannelResult.Fail(accessTokenResult.ErrorMessage);
                }

                var mimeMessage = BuildMimeMessage(recipient, subject, htmlBody);
                var rawMessage = ConvertToBase64Url(mimeMessage);

                using var request = new HttpRequestMessage(HttpMethod.Post, GmailSendEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenResult.AccessToken);
                request.Content = JsonContent.Create(new GmailSendRequest { Raw = rawMessage });

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return NotificationChannelResult.Fail($"Gmail API error: {(int)response.StatusCode} - {body}");
                }

                return NotificationChannelResult.Ok($"Gmail API accepted: {body}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gmail API send failed. To={To}", recipient);
                return NotificationChannelResult.Fail(ex.Message);
            }
        }

        public static bool IsConfigured(GmailApiOptions options)
        {
            return !string.IsNullOrWhiteSpace(options.ClientId) &&
                   !string.IsNullOrWhiteSpace(options.ClientSecret) &&
                   !string.IsNullOrWhiteSpace(options.RefreshToken) &&
                   !string.IsNullOrWhiteSpace(options.FromEmail);
        }

        private MimeMessage BuildMimeMessage(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            var fromName = _notificationOptions.Email.FromName?.Trim() ?? "DigiBook";
            var fromEmail = _gmailApiOptions.FromEmail.Trim();

            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };
            return message;
        }

        private static string ConvertToBase64Url(MimeMessage message)
        {
            using var stream = new MemoryStream();
            message.WriteTo(stream);
            var bytes = stream.ToArray();
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private async Task<TokenExchangeResult> ExchangeRefreshTokenAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _gmailApiOptions.ClientId,
                    ["client_secret"] = _gmailApiOptions.ClientSecret,
                    ["refresh_token"] = _gmailApiOptions.RefreshToken,
                    ["grant_type"] = "refresh_token"
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = $"Gmail token exchange failed: {(int)response.StatusCode} - {body}"
                };
            }

            var payload = await response.Content.ReadFromJsonAsync<GmailTokenResponse>(cancellationToken: cancellationToken);
            if (payload == null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = "Gmail token exchange returned empty access_token."
                };
            }

            return new TokenExchangeResult
            {
                Success = true,
                AccessToken = payload.AccessToken
            };
        }

        private sealed class GmailSendRequest
        {
            [JsonPropertyName("raw")]
            public string Raw { get; set; } = string.Empty;
        }

        private sealed class GmailTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
        }

        private sealed class TokenExchangeResult
        {
            public bool Success { get; set; }
            public string AccessToken { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}
