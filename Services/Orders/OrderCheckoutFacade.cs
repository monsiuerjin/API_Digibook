using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Interfaces.Payment;
using API_DigiBook.Models;
using API_DigiBook.Commands;
using API_DigiBook.Interfaces.Commands;
using API_DigiBook.Commands.Orders;
using API_DigiBook.Notifications;
using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Factories;

namespace API_DigiBook.Services.Orders
{
    public class OrderCheckoutFacade : IOrderCheckoutFacade
    {
        private readonly IOrderRepository _orderRepository;
        private readonly CommandInvoker _commandInvoker;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly PaymentServiceFactory _paymentServiceFactory;
        private readonly ILogger<OrderCheckoutFacade> _logger;
        private readonly IConfiguration _configuration;

        public OrderCheckoutFacade(
            IOrderRepository orderRepository,
            CommandInvoker commandInvoker,
            INotificationPublisher notificationPublisher,
            PaymentServiceFactory paymentServiceFactory,
            ILogger<OrderCheckoutFacade> logger,
            IConfiguration configuration)
        {
            _orderRepository = orderRepository;
            _commandInvoker = commandInvoker;
            _notificationPublisher = notificationPublisher;
            _paymentServiceFactory = paymentServiceFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<CommandResult> CheckoutAsync(Order order)
        {
            _logger.LogInformation("Starting checkout process for user {UserId}", order.UserId);

            try
            {
                // 1. Thực thi CreateOrderCommand để lưu đơn hàng vào DB
                var createOrderCommand = new CreateOrderCommand(order, _orderRepository, _logger);
                var result = await _commandInvoker.ExecuteAsync(createOrderCommand);

                if (!result.Success)
                {
                    return result;
                }

                var createdOrder = result.Data as Order;
                if (createdOrder == null)
                {
                    return CommandResult.FailureResult("Failed to retrieve created order data");
                }

                // 2. Xử lý thanh toán nếu là PayOS
                if (createdOrder.Payment != null && 
                    (createdOrder.Payment.Provider?.ToUpper() == "PAYOS" || 
                     createdOrder.Payment.Method?.ToUpper() == "PAYOS"))
                {
                    var paymentService = _paymentServiceFactory.CreatePaymentService("PAYOS");
                    if (paymentService != null)
                    {
                        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5173";
                        
                        var paymentRequest = new PaymentRequest
                        {
                            OrderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), // PayOS requires unique numeric order code
                            Amount = createdOrder.Payment.Total,
                            Description = $"Thanh toan don hang {createdOrder.Id}",
                            ReturnUrl = $"{baseUrl}/order-success?orderId={createdOrder.Id}",
                            CancelUrl = $"{baseUrl}/payment-cancel?orderId={createdOrder.Id}",
                            Customer = new Models.CustomerInfo
                            {
                                Name = createdOrder.Customer.Name,
                                Email = createdOrder.Customer.Email,
                                Phone = createdOrder.Customer.Phone
                            },
                            Items = createdOrder.Items.Select(i => new Models.PaymentItem
                            {
                                Name = i.Title,
                                Quantity = i.Quantity,
                                Price = i.PriceAtPurchase
                            }).ToList()
                        };

                        var paymentResponse = await paymentService.CreatePaymentLinkAsync(paymentRequest);
                        if (paymentResponse.Success)
                        {
                            // Cập nhật thông tin thanh toán vào đơn hàng
                            createdOrder.Payment.CheckoutUrl = paymentResponse.CheckoutUrl;
                            createdOrder.Payment.TransactionId = paymentResponse.PaymentLinkId;
                            
                            var updates = new Dictionary<string, object?>
                            {
                                { "payment.checkoutUrl", paymentResponse.CheckoutUrl },
                                { "payment.transactionId", paymentResponse.PaymentLinkId }
                            };
                            
                            await _orderRepository.UpdateFieldsAsync(createdOrder.Id, updates);
                            _logger.LogInformation("Payment link created for Order {OrderId}: {CheckoutUrl}", createdOrder.Id, paymentResponse.CheckoutUrl);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create PayOS payment link for Order {OrderId}: {Message}", createdOrder.Id, paymentResponse.Message);
                            // Vẫn tiếp tục quy trình, user có thể thanh toán sau hoặc báo lỗi tùy nghiệp vụ
                        }
                    }
                }

                // 3. Gửi thông báo (Notification)
                try
                {
                    var notificationEvent = NotificationEventFactory.ForOrderCreated(createdOrder);
                    await _notificationPublisher.PublishAsync(notificationEvent);
                }
                catch (Exception nEx)
                {
                    // Lỗi gửi thông báo không nên làm hỏng quy trình thanh toán
                    _logger.LogWarning(nEx, "Error publishing notification for Order {OrderId}", createdOrder.Id);
                }

                _logger.LogInformation("Checkout completed successfully for Order {OrderId}", createdOrder.Id);
                return CommandResult.SuccessResult("Checkout completed successfully", createdOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout process");
                return CommandResult.FailureResult("An error occurred during checkout", ex.Message);
            }
        }
    }
}
