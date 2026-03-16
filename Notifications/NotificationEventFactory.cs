using API_DigiBook.Models;
using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Notifications
{
    public static class NotificationEventFactory
    {
        private static NotificationEvent CreateBaseEvent(Order order)
        {
            var totalItems = order.Items?.Sum(i => Math.Max(1, i.Quantity)) ?? 0;
            var itemSummary = order.Items == null || order.Items.Count == 0
                ? "Khong co san pham"
                : string.Join(", ",
                    order.Items
                        .Take(3)
                        .Select(i => $"{i.Title} x{i.Quantity}"));

            if (order.Items != null && order.Items.Count > 3)
            {
                itemSummary += $", +{order.Items.Count - 3} san pham khac";
            }

            return new NotificationEvent
            {
                CorrelationId = order.Id,
                OrderId = order.Id,
                OrderDate = order.Date,
                UserId = order.UserId,
                CustomerName = order.Customer?.Name ?? string.Empty,
                CustomerPhone = order.Customer?.Phone ?? string.Empty,
                CustomerAddress = order.Customer?.Address ?? string.Empty,
                CustomerEmail = order.Customer?.Email ?? string.Empty,
                PaymentMethod = order.Payment?.Method ?? string.Empty,
                PaymentProvider = order.Payment?.Provider ?? string.Empty,
                PaymentStatus = order.Payment?.Status ?? string.Empty,
                TransactionId = order.Payment?.TransactionId ?? string.Empty,
                ItemCount = totalItems,
                ItemSummary = itemSummary,
                Subtotal = order.Payment?.Subtotal ?? 0,
                Shipping = order.Payment?.Shipping ?? 0,
                CouponDiscount = order.Payment?.CouponDiscount ?? 0,
                Total = order.Payment?.Total ?? 0,
                NewStatus = order.Status,
                OccurredAtUtc = DateTime.UtcNow
            };
        }

        public static NotificationEvent ForOrderCreated(Order order)
        {
            var notificationEvent = CreateBaseEvent(order);
            notificationEvent.EventId = $"order-created:{order.Id}";
            notificationEvent.EventType = NotificationEventTypes.OrderCreated;
            return notificationEvent;
        }

        public static NotificationEvent ForPaymentPaid(Order order)
        {
            var notificationEvent = CreateBaseEvent(order);
            notificationEvent.EventId = $"payment-paid:{order.Id}";
            notificationEvent.EventType = NotificationEventTypes.PaymentPaid;
            return notificationEvent;
        }

        public static NotificationEvent ForOrderStatusChanged(Order order, string? oldStatus)
        {
            var notificationEvent = CreateBaseEvent(order);
            notificationEvent.EventId = $"order-status-changed:{order.Id}:{order.Status}";
            notificationEvent.EventType = NotificationEventTypes.OrderStatusChanged;
            notificationEvent.OldStatus = oldStatus;
            return notificationEvent;
        }
    }
}
