namespace API_DigiBook.Notifications.Configuration
{
    public class GmailApiOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
    }
}
