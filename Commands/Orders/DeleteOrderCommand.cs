using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Commands;

namespace API_DigiBook.Commands.Orders
{
    /// <summary>
    /// Command to delete an order
    /// </summary>
    public class DeleteOrderCommand : ICommand<CommandResult>
    {
        private readonly string _orderId;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger;

        public DeleteOrderCommand(
            string orderId,
            IOrderRepository orderRepository,
            ILogger logger)
        {
            _orderId = orderId;
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

                // Validate: Only allow deletion of cancelled orders or processing orders
                if (order.Status != "Đã hủy" && order.Status != "Đang xử lý")
                {
                    return CommandResult.FailureResult(
                        $"Cannot delete order with status '{order.Status}'. " +
                        "Only 'Đã hủy' or 'Đang xử lý' orders can be deleted."
                    );
                }

                // Delete order
                var deleted = await _orderRepository.DeleteAsync(_orderId);

                if (!deleted)
                {
                    return CommandResult.FailureResult("Failed to delete order");
                }

                _logger.LogInformation("Order {OrderId} deleted successfully", _orderId);

                return CommandResult.SuccessResult("Order deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", _orderId);
                return CommandResult.FailureResult("Error deleting order", ex.Message);
            }
        }
    }
}
