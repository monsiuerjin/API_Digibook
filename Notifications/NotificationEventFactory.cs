using API_DigiBook.Models;
using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Notifications
{
    public static class NotificationEventFactory
    {
        public static NotificationEvent ForOrderCreated(Order order)
        {
            return new NotificationEvent
            {
                EventId = $"order-created:{order.Id}",
                EventType = NotificationEventTypes.OrderCreated,
                CorrelationId = order.Id,
                OrderId = order.Id,
                UserId = order.UserId,
                CustomerEmail = order.Customer?.Email ?? string.Empty,
                NewStatus = order.Status,
                OccurredAtUtc = DateTime.UtcNow
            };
        }

        public static NotificationEvent ForPaymentPaid(Order order)
        {
            return new NotificationEvent
            {
                EventId = $"payment-paid:{order.Id}",
                EventType = NotificationEventTypes.PaymentPaid,
                CorrelationId = order.Id,
                OrderId = order.Id,
                UserId = order.UserId,
                CustomerEmail = order.Customer?.Email ?? string.Empty,
                NewStatus = order.Status,
                OccurredAtUtc = DateTime.UtcNow
            };
        }

        public static NotificationEvent ForOrderStatusChanged(Order order, string? oldStatus)
        {
            return new NotificationEvent
            {
                EventId = $"order-status-changed:{order.Id}:{order.Status}",
                EventType = NotificationEventTypes.OrderStatusChanged,
                CorrelationId = order.Id,
                OrderId = order.Id,
                UserId = order.UserId,
                CustomerEmail = order.Customer?.Email ?? string.Empty,
                OldStatus = oldStatus,
                NewStatus = order.Status,
                OccurredAtUtc = DateTime.UtcNow
            };
        }
    }
}
