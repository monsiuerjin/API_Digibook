using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Commands;
using Google.Cloud.Firestore;

namespace API_DigiBook.Commands.Orders
{
    /// <summary>
    /// Command to cancel an order
    /// </summary>
    public class CancelOrderCommand : ICommand<CommandResult>
    {
        private readonly string _orderId;
        private readonly string _cancelReason;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger;

        public CancelOrderCommand(
            string orderId,
            string cancelReason,
            IOrderRepository orderRepository,
            ILogger logger)
        {
            _orderId = orderId;
            _cancelReason = cancelReason;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            try
            {
                // Check if order exists
                var order = await _orderRepository.GetByIdAsync(_orderId);
                if (order == null)
                {
                    return CommandResult.FailureResult($"Order with ID '{_orderId}' not found");
                }

                // Check if order can be cancelled
                if (order.Status == "Đã giao")
                {
                    return CommandResult.FailureResult("Cannot cancel an order that has been delivered");
                }

                if (order.Status == "Đã hủy")
                {
                    return CommandResult.FailureResult("Order is already cancelled");
                }

                // Cancel order
                var updates = new Dictionary<string, object?>
                {
                    { "status", "Đã hủy" },
                    { "statusStep", 4 },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                // Add cancel reason to customer note if provided
                if (!string.IsNullOrEmpty(_cancelReason))
                {
                    var note = string.IsNullOrEmpty(order.Customer.Note) 
                        ? $"Lý do hủy: {_cancelReason}"
                        : $"{order.Customer.Note}\nLý do hủy: {_cancelReason}";
                    
                    updates.Add("customer.note", note);
                }

                var updated = await _orderRepository.UpdateFieldsAsync(_orderId, updates);

                if (!updated)
                {
                    return CommandResult.FailureResult("Failed to cancel order");
                }

                _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", _orderId, _cancelReason);

                return CommandResult.SuccessResult("Order cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", _orderId);
                return CommandResult.FailureResult("Error cancelling order", ex.Message);
            }
        }
    }
}
