using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace API_DigiBook.Notifications.Channels
{
    public class TelegramNotificationChannel
    {
        private readonly HttpClient _httpClient;
        private readonly NotificationOptions _options;

        public TelegramNotificationChannel(HttpClient httpClient, IOptions<NotificationOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _httpClient.Timeout = TimeSpan.FromMilliseconds(_options.Telegram.TimeoutMilliseconds);
        }

        public async Task<NotificationChannelResult> SendAsync(
            string chatId,
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return NotificationChannelResult.Fail("Telegram chatId is empty.");
            }

            if (string.IsNullOrWhiteSpace(_options.Telegram.BotToken))
            {
                return NotificationChannelResult.Fail("Telegram bot token is missing.");
            }

            try
            {
                var endpoint = $"https://api.telegram.org/bot{_options.Telegram.BotToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "HTML",
                    disable_web_page_preview = true
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return NotificationChannelResult.Fail($"Telegram API error: {(int)response.StatusCode} - {responseBody}");
                }

                return NotificationChannelResult.Ok(responseBody);
            }
            catch (Exception ex)
            {
                return NotificationChannelResult.Fail(ex.Message);
            }
        }
    }
}
