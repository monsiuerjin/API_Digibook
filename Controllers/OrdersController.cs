using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Repositories;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderRepository orderRepository, ILogger<OrdersController> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
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
        /// Create a new order
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            try
            {
                order.CreatedAt = Timestamp.GetCurrentTimestamp();
                order.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var orderId = await _orderRepository.AddAsync(order, order.Id);
                order.Id = orderId;

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, new
                {
                    success = true,
                    message = "Order created successfully",
                    data = order
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating order",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing order
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(string id, [FromBody] Order order)
        {
            try
            {
                order.Id = id;
                order.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var updated = await _orderRepository.UpdateAsync(id, order);

                if (!updated)
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
                    message = "Order updated successfully",
                    data = order
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating order",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "status", request.Status },
                    { "statusStep", request.StatusStep },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                var updated = await _orderRepository.UpdateFieldsAsync(id, updates);

                if (!updated)
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
                    message = "Order status updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating order status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete an order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(string id)
        {
            try
            {
                var deleted = await _orderRepository.DeleteAsync(id);

                if (!deleted)
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
                    message = "Order deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting order",
                    error = ex.Message
                });
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public int StatusStep { get; set; }
    }
}
