using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Commands;
using Google.Cloud.Firestore;
using API_DigiBook.Factories;
using API_DigiBook.Interfaces.States;

namespace API_DigiBook.Commands.Orders
{
    /// <summary>
    /// Command to update order status
    /// </summary>
    public class UpdateOrderStatusCommand : ICommand<CommandResult>
    {
        private readonly string _orderId;
        private readonly string _newStatus;
        private readonly int _newStatusStep;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger;

        public UpdateOrderStatusCommand(
            string orderId,
            string newStatus,
            int newStatusStep,
            IOrderRepository orderRepository,
            ILogger logger)
        {
            _orderId = orderId;
            _newStatus = newStatus;
            _newStatusStep = newStatusStep;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            try
            {
                // Validate and normalize status using Status Step to prevent Unicode normalization issues (NFC vs NFD)
                var statusStepMap = new Dictionary<int, string>
                {
                    { 0, "Đang xử lý" },
                    { 1, "Đã xác nhận" },
                    { 5, "Đang đóng gói" },
                    { 2, "Đang giao" },
                    { 3, "Đã giao" },
                    { 4, "Đã hủy" },
                    { 6, "Giao thất bại" }
                };

                if (!statusStepMap.TryGetValue(_newStatusStep, out var exactStatus))
                {
                    exactStatus = _newStatus; // Fallback
                    var validStatuses = statusStepMap.Values.ToArray();
                    if (!validStatuses.Contains(_newStatus))
                    {
                        return CommandResult.FailureResult($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");
                    }
                }

                // Check if order exists
                var order = await _orderRepository.GetByIdAsync(_orderId);
                if (order == null)
                {
                    return CommandResult.FailureResult($"Order with ID '{_orderId}' not found");
                }

                // Validate status transition using State Pattern
                // Chuẩn hóa chuỗi Tiếng Việt về dạng NFC để đảm bảo so sánh chính xác giữa DB và API
                string currentStatusNormalized = (order.Status ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
                string newStatusNormalized = exactStatus.Normalize(System.Text.NormalizationForm.FormC);

                IOrderState currentState = OrderStateFactory.GetState(currentStatusNormalized);
                if (!currentState.CanTransitionTo(newStatusNormalized))
                {
                    return CommandResult.FailureResult($"Không thể chuyển trạng thái từ '{order.Status}' sang '{exactStatus}' (Sai quy trình nghiệp vụ).");
                }

                // Update status
                var updates = new Dictionary<string, object?>
                {
                    { "status", exactStatus },
                    { "statusStep", _newStatusStep },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                var updated = await _orderRepository.UpdateFieldsAsync(_orderId, updates);

                if (!updated)
                {
                    return CommandResult.FailureResult("Failed to update order status");
                }

                _logger.LogInformation("Order {OrderId} status updated from '{OldStatus}' to '{NewStatus}'",
                    _orderId, order.Status, exactStatus);

                return CommandResult.SuccessResult($"Order status updated to '{exactStatus}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", _orderId);
                return CommandResult.FailureResult("Error updating order status", ex.Message);
            }
        }
    }
}
