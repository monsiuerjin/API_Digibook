using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Commands;
using Google.Cloud.Firestore;

namespace API_DigiBook.Commands.Orders
{
    /// <summary>
    /// Command to create a new order
    /// </summary>
    public class CreateOrderCommand : ICommand<CommandResult>
    {
        private readonly Order _order;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger;

        public CreateOrderCommand(
            Order order,
            IOrderRepository orderRepository,
            ILogger logger)
        {
            _order = order;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            try
            {
                // Validate order
                if (string.IsNullOrEmpty(_order.UserId))
                {
                    return CommandResult.FailureResult("User ID is required");
                }

                if (_order.Items == null || !_order.Items.Any())
                {
                    return CommandResult.FailureResult("Order must have at least one item");
                }

                // Set timestamps
                _order.CreatedAt = Timestamp.GetCurrentTimestamp();
                _order.UpdatedAt = Timestamp.GetCurrentTimestamp();
                
                // Set initial status
                if (string.IsNullOrEmpty(_order.Status))
                {
                    _order.Status = "Đang xử lý";
                    _order.StatusStep = 0;
                }

                // Set order date
                if (string.IsNullOrEmpty(_order.Date))
                {
                    _order.Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm", 
                        new System.Globalization.CultureInfo("vi-VN"));
                }

                // Create order
                var orderId = await _orderRepository.AddAsync(_order, _order.Id);
                _order.Id = orderId;

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", 
                    orderId, _order.UserId);

                return CommandResult.SuccessResult("Order created successfully", _order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return CommandResult.FailureResult("Error creating order", ex.Message);
            }
        }
    }
}
