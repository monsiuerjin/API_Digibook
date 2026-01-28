using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Commands;
using Google.Cloud.Firestore;

namespace API_DigiBook.Commands.Orders
{
    /// <summary>
    /// Command to update an entire order
    /// </summary>
    public class UpdateOrderCommand : ICommand<CommandResult>
    {
        private readonly string _orderId;
        private readonly Order _order;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger;

        public UpdateOrderCommand(
            string orderId,
            Order order,
            IOrderRepository orderRepository,
            ILogger logger)
        {
            _orderId = orderId;
            _order = order;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            try
            {
                // Check if order exists
                var existingOrder = await _orderRepository.GetByIdAsync(_orderId);
                if (existingOrder == null)
                {
                    return CommandResult.FailureResult($"Order with ID '{_orderId}' not found");
                }

                // Validate: Cannot edit delivered or cancelled orders
                if (existingOrder.Status == "Đã giao" || existingOrder.Status == "Đã hủy")
                {
                    return CommandResult.FailureResult($"Cannot update order with status '{existingOrder.Status}'");
                }

                // Update order
                _order.Id = _orderId;
                _order.UpdatedAt = Timestamp.GetCurrentTimestamp();
                _order.CreatedAt = existingOrder.CreatedAt; // Keep original creation time

                var updated = await _orderRepository.UpdateAsync(_orderId, _order);

                if (!updated)
                {
                    return CommandResult.FailureResult("Failed to update order");
                }

                _logger.LogInformation("Order {OrderId} updated successfully", _orderId);

                return CommandResult.SuccessResult("Order updated successfully", _order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", _orderId);
                return CommandResult.FailureResult("Error updating order", ex.Message);
            }
        }
    }
}
