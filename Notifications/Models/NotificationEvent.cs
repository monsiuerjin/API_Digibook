namespace API_DigiBook.Notifications.Models
{
    public class NotificationEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        public string EventType { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentProvider { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string ItemSummary { get; set; } = string.Empty;
        public double Subtotal { get; set; }
        public double Shipping { get; set; }
        public double CouponDiscount { get; set; }
        public double Total { get; set; }
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
