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
                // Validate status
                var validStatuses = new[] { "Đang xử lý", "Đã xác nhận", "Đang đóng gói", "Đang giao", "Đã giao", "Đã hủy", "Giao thất bại" };
                if (!validStatuses.Contains(_newStatus))
                {
                    return CommandResult.FailureResult($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");
                }

                // Check if order exists
                var order = await _orderRepository.GetByIdAsync(_orderId);
                if (order == null)
                {
                    return CommandResult.FailureResult($"Order with ID '{_orderId}' not found");
                }

                // Validate status transition using State Pattern
                IOrderState currentState = OrderStateFactory.GetState(order.Status);
                if (!currentState.CanTransitionTo(_newStatus))
                {
                    return CommandResult.FailureResult($"Cannot change status from '{order.Status}' to '{_newStatus}'");
                }

                // Update status
                var updates = new Dictionary<string, object?>
                {
                    { "status", _newStatus },
                    { "statusStep", _newStatusStep },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                var updated = await _orderRepository.UpdateFieldsAsync(_orderId, updates);

                if (!updated)
                {
                    return CommandResult.FailureResult("Failed to update order status");
                }

                _logger.LogInformation("Order {OrderId} status updated from '{OldStatus}' to '{NewStatus}'",
                    _orderId, order.Status, _newStatus);

                return CommandResult.SuccessResult($"Order status updated to '{_newStatus}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", _orderId);
                return CommandResult.FailureResult("Error updating order status", ex.Message);
            }
        }
    }
}
