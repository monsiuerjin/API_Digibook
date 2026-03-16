using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Commands;
using API_DigiBook.Commands.Orders;
using API_DigiBook.Notifications;
using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrdersController> _logger;
        private readonly CommandInvoker _commandInvoker;
        private readonly INotificationPublisher _notificationPublisher;

        public OrdersController(
            IOrderRepository orderRepository, 
            ILogger<OrdersController> logger,
            CommandInvoker commandInvoker,
            INotificationPublisher notificationPublisher)
        {
            _orderRepository = orderRepository;
            _logger = logger;
            _commandInvoker = commandInvoker;
            _notificationPublisher = notificationPublisher;
        }

        /// <summary>
        /// Get all orders
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    count = orders.Count(),
                    data = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving orders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(id);

                if (order == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Order with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = order
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving order",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get orders by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUserId(string userId)
        {
            try
            {
                var orders = await _orderRepository.GetByUserIdAsync(userId);

                return Ok(new
                {
                    success = true,
                    count = orders.Count(),
                    data = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by user ID: {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving orders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get orders by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetOrdersByStatus(string status)
        {
            try
            {
                var orders = await _orderRepository.GetByStatusAsync(status);

                return Ok(new
                {
                    success = true,
                    count = orders.Count(),
                    data = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by status: {Status}", status);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving orders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get recent orders
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int count = 10)
        {
            try
            {
                var orders = await _orderRepository.GetRecentOrdersAsync(count);

                return Ok(new
                {
                    success = true,
                    count = orders.Count(),
                    data = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving orders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new order using Command Pattern
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            var command = new CreateOrderCommand(order, _orderRepository, _logger);
            var result = await _commandInvoker.ExecuteAsync(command);

            if (result.Success)
            {
                var createdOrder = result.Data as Order;

                if (createdOrder != null)
                {
                    var notificationEvent = NotificationEventFactory.ForOrderCreated(createdOrder);
                    await PublishSafelyAsync(notificationEvent);
                }

                return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder?.Id }, new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.Message,
                error = result.Error
            });
        }

        /// <summary>
        /// Update an existing order using Command Pattern
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(string id, [FromBody] Order order)
        {
            var command = new UpdateOrderCommand(id, order, _orderRepository, _logger);
            var result = await _commandInvoker.ExecuteAsync(command);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                });
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.Message,
                error = result.Error
            });
        }

        /// <summary>
        /// Update order status using Command Pattern
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            var previousOrder = await _orderRepository.GetByIdAsync(id);

            var command = new UpdateOrderStatusCommand(
                id, 
                request.Status, 
                request.StatusStep, 
                _orderRepository, 
                _logger);
            
            var result = await _commandInvoker.ExecuteAsync(command);

            if (result.Success)
            {
                var updatedOrder = await _orderRepository.GetByIdAsync(id);
                if (updatedOrder != null)
                {
                    var notificationEvent = NotificationEventFactory.ForOrderStatusChanged(updatedOrder, previousOrder?.Status);
                    await PublishSafelyAsync(notificationEvent);
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message
                });
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.Message,
                error = result.Error
            });
        }

        private async Task PublishSafelyAsync(NotificationEvent notificationEvent)
        {
            try
            {
                await _notificationPublisher.PublishAsync(notificationEvent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Notification publish failed for EventType={EventType}, EventId={EventId}",
                    notificationEvent.EventType,
                    notificationEvent.EventId);
            }
        }

        /// <summary>
        /// Delete an order using Command Pattern
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(string id)
        {
            var command = new DeleteOrderCommand(id, _orderRepository, _logger);
            var result = await _commandInvoker.ExecuteAsync(command);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message
                });
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.Message,
                error = result.Error
            });
        }

        /// <summary>
        /// Cancel an order using Command Pattern
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id, [FromBody] CancelOrderRequest request)
        {
            var command = new CancelOrderCommand(id, request.Reason ?? "", _orderRepository, _logger);
            var result = await _commandInvoker.ExecuteAsync(command);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message
                });
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message,
                    error = result.Error
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.Message,
                error = result.Error
            });
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public int StatusStep { get; set; }
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }
}
