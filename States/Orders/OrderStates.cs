using API_DigiBook.Interfaces.States;
using System.Linq;

namespace API_DigiBook.States.Orders
{
    public class PendingState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus)
        {
            var validTransitions = new[] { "Đã xác nhận", "Đã hủy" };
            string normalizedNext = (nextStatus ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
            return validTransitions.Any(t => t.Normalize(System.Text.NormalizationForm.FormC) == normalizedNext);
        }

        public string GetStatusName() => "Đang xử lý";
    }

    public class ConfirmedState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus)
        {
            var validTransitions = new[] { "Đang đóng gói", "Đang giao", "Đã hủy" };
            string normalizedNext = (nextStatus ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
            return validTransitions.Any(t => t.Normalize(System.Text.NormalizationForm.FormC) == normalizedNext);
        }

        public string GetStatusName() => "Đã xác nhận";
    }

    public class PackingState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus)
        {
            var validTransitions = new[] { "Đang giao", "Đã hủy" };
            string normalizedNext = (nextStatus ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
            return validTransitions.Any(t => t.Normalize(System.Text.NormalizationForm.FormC) == normalizedNext);
        }

        public string GetStatusName() => "Đang đóng gói";
    }

    public class ShippingState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus)
        {
            // Now transitioning to "Giao thất bại" instead of "Đã hủy" when failing to deliver
            var validTransitions = new[] { "Đã giao", "Giao thất bại", "Đã hủy" };
            string normalizedNext = (nextStatus ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
            return validTransitions.Any(t => t.Normalize(System.Text.NormalizationForm.FormC) == normalizedNext);
        }

        public string GetStatusName() => "Đang giao";
    }

    public class DeliveredState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus) => false;

        public string GetStatusName() => "Đã giao";
    }

    public class CanceledState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus) => false;

        public string GetStatusName() => "Đã hủy";
    }

    public class FailedDeliveryState : IOrderState
    {
        public bool CanTransitionTo(string nextStatus) => false;

        public string GetStatusName() => "Giao thất bại";
    }
}
