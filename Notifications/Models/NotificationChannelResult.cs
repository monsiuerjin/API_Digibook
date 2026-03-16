namespace API_DigiBook.Notifications.Models
{
    public class NotificationChannelResult
    {
        public bool Success { get; set; }
        public string ProviderResponse { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public static NotificationChannelResult Ok(string providerResponse = "")
        {
            return new NotificationChannelResult
            {
                Success = true,
                ProviderResponse = providerResponse
            };
        }

        public static NotificationChannelResult Fail(string errorMessage)
        {
            return new NotificationChannelResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
