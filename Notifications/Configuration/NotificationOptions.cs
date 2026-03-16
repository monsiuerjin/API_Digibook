namespace API_DigiBook.Notifications.Configuration
{
    public class NotificationOptions
    {
        public bool EnableEmail { get; set; } = true;
        public bool EnableTelegram { get; set; } = true;
        public int RetryCount { get; set; } = 1;
        public int RetryDelayMilliseconds { get; set; } = 300;
        public EmailNotificationOptions Email { get; set; } = new();
        public TelegramNotificationOptions Telegram { get; set; } = new();
    }

    public class EmailNotificationOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "DigiBook";
        public bool EnableSsl { get; set; } = true;
        public int TimeoutMilliseconds { get; set; } = 5000;
    }

    public class TelegramNotificationOptions
    {
        public string BotToken { get; set; } = string.Empty;
        public string BotUsername { get; set; } = string.Empty;
        public int TimeoutMilliseconds { get; set; } = 5000;
    }
}
