using API_DigiBook.Factories;
using API_DigiBook.Models;
using API_DigiBook.Repositories;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Services;
using API_DigiBook.Notifications;
using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Notifications.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentServiceFactory _paymentFactory;
        private readonly IOrderRepository _orderRepository;
        private readonly FirestoreDb _db;
        private readonly ILogger<PaymentController> _logger;
        private readonly INotificationPublisher _notificationPublisher;

        public PaymentController(
            PaymentServiceFactory paymentFactory,
            IOrderRepository orderRepository,
            INotificationPublisher notificationPublisher,
            ILogger<PaymentController> logger)
        {
            _paymentFactory = paymentFactory;
            _orderRepository = orderRepository;
            _db = FirebaseService.GetFirestoreDb();
            _notificationPublisher = notificationPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Tạo payment link cho đơn hàng
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<PaymentResponse>> CreatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.OrderId))
                    return BadRequest(new { message = "OrderId is required" });

                // Lấy thông tin đơn hàng
                var order = await _orderRepository.GetByIdAsync(request.OrderId);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Nếu là COD, không cần tạo payment link
                if (order.Payment.Provider.ToUpper() == "COD")
                {
                    return Ok(new PaymentResponse
                    {
                        Success = true,
                        Message = "COD order, no payment link needed",
                        CheckoutUrl = string.Empty
                    });
                }

                // Tạo payment service từ factory
                var paymentService = _paymentFactory.CreatePaymentService(order.Payment.Provider);
                if (paymentService == null)
                    return BadRequest(new { message = "Invalid payment provider" });

                // Tạo payment link
                var paymentResponse = await paymentService.CreatePaymentLinkAsync(request);

                if (paymentResponse.Success)
                {
                    // Lưu transaction vào Firestore
                    var transaction = new PaymentTransaction
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderId = request.OrderId,
                        OrderCode = request.OrderCode,
                        Provider = paymentService.GetProviderName(),
                        PaymentLinkId = paymentResponse.PaymentLinkId,
                        Amount = request.Amount,
                        Status = "PENDING",
                        CheckoutUrl = paymentResponse.CheckoutUrl,
                        CreatedAt = Timestamp.GetCurrentTimestamp(),
                        UpdatedAt = Timestamp.GetCurrentTimestamp()
                    };

                    await _db.Collection("PaymentTransactions").Document(transaction.Id).SetAsync(transaction);

                    // Cập nhật order với checkout URL
                    order.Payment.CheckoutUrl = paymentResponse.CheckoutUrl;
                    order.Payment.TransactionId = paymentResponse.PaymentLinkId;
                    order.Payment.Status = "PENDING";
                    order.UpdatedAt = Timestamp.GetCurrentTimestamp();

                    await _orderRepository.UpdateAsync(order.Id, order);
                }

                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Xác thực trạng thái thanh toán
        /// </summary>
        [HttpGet("verify/{orderId}")]
        public async Task<ActionResult<PaymentVerification>> VerifyPayment(string orderId)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Nếu là COD
                if (order.Payment.Provider.ToUpper() == "COD")
                {
                    return Ok(new PaymentVerification
                    {
                        IsValid = true,
                        Status = "PAID",
                        OrderId = orderId,
                        Message = "COD order"
                    });
                }

                // Tạo payment service từ factory
                var paymentService = _paymentFactory.CreatePaymentService(order.Payment.Provider);
                if (paymentService == null)
                    return BadRequest(new { message = "Invalid payment provider" });

                // Cần lấy paymentLinkId để verify với PayOS
                string paymentLinkId = order.Payment.TransactionId;
                
                // Nếu không có TransactionId, thử tìm từ PaymentTransactions
                if (string.IsNullOrEmpty(paymentLinkId))
                {
                    var txQuery = _db.Collection("PaymentTransactions")
                        .WhereEqualTo("OrderId", orderId)
                        .Limit(1);
                    var txSnapshot = await txQuery.GetSnapshotAsync();
                    
                    if (txSnapshot.Documents.Any())
                    {
                        var txDoc = txSnapshot.Documents.First();
                        var transaction = txDoc.ConvertTo<PaymentTransaction>();
                        paymentLinkId = transaction.PaymentLinkId;
                    }
                }

                _logger.LogInformation($"Verifying payment for order {orderId} with paymentLinkId: {paymentLinkId}");

                // Verify payment
                var verification = await paymentService.VerifyPaymentAsync(paymentLinkId);

                // Cập nhật order status nếu đã thanh toán
                if (verification.IsValid && verification.Status == "PAID")
                {
                    _logger.LogInformation($"Payment PAID for order {orderId}. Updating order status...");
                    
                    order.Payment.Status = "PAID";
                    order.Status = "Đã xác nhận"; // Update main order status
                    order.StatusStep = 1; // Move to next step
                    order.UpdatedAt = Timestamp.GetCurrentTimestamp();
                    await _orderRepository.UpdateAsync(order.Id, order);

                    _logger.LogInformation($"Order {orderId} updated successfully to 'Đã xác nhận'");

                    // Cập nhật transaction
                    var query = _db.Collection("PaymentTransactions")
                        .WhereEqualTo("OrderId", orderId)
                        .Limit(1);
                    var snapshot = await query.GetSnapshotAsync();

                    if (snapshot.Documents.Any())
                    {
                        var doc = snapshot.Documents.First();
                        var transaction = doc.ConvertTo<PaymentTransaction>();
                        transaction.Status = "PAID";
                        transaction.PaidAt = Timestamp.GetCurrentTimestamp();
                        transaction.UpdatedAt = Timestamp.GetCurrentTimestamp();
                        await _db.Collection("PaymentTransactions").Document(doc.Id).SetAsync(transaction);
                    }

                    var notificationEvent = NotificationEventFactory.ForPaymentPaid(order);
                    await PublishSafelyAsync(notificationEvent);
                }

                return Ok(verification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Webhook callback từ payment gateway
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] Dictionary<string, string> callbackData)
        {
            try
            {
                _logger.LogInformation("Received payment webhook: {data}", callbackData);

                if (!callbackData.ContainsKey("orderCode"))
                    return BadRequest(new { message = "Missing orderCode" });

                var orderCode = callbackData["orderCode"];
                
                // Tìm transaction theo orderCode
                var query = _db.Collection("PaymentTransactions")
                    .WhereEqualTo("OrderCode", orderCode)
                    .Limit(1);
                var snapshot = await query.GetSnapshotAsync();

                if (!snapshot.Documents.Any())
                    return NotFound(new { message = "Transaction not found for orderCode: " + orderCode });

                var transactionDoc = snapshot.Documents.First();
                var transaction = transactionDoc.ConvertTo<PaymentTransaction>();

                // Lấy order từ transaction
                var order = await _orderRepository.GetByIdAsync(transaction.OrderId);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Verify webhook signature
                var paymentService = _paymentFactory.CreatePaymentService(order.Payment.Provider);
                if (paymentService == null)
                    return BadRequest(new { message = "Invalid payment provider" });

                var isValid = await paymentService.HandleCallbackAsync(callbackData);
                if (!isValid)
                    return Unauthorized(new { message = "Invalid signature" });

                // Cập nhật order status
                if (callbackData.ContainsKey("status"))
                {
                    var status = callbackData["status"];
                    order.Payment.Status = status;
                    
                    if (status == "PAID")
                    {
                        order.Status = "Đã thanh toán";
                    }
                    else if (status == "CANCELLED")
                    {
                        order.Status = "Đã hủy";
                    }

                    order.UpdatedAt = Timestamp.GetCurrentTimestamp();
                    await _orderRepository.UpdateAsync(order.Id, order);

                    if (status == "PAID")
                    {
                        var notificationEvent = NotificationEventFactory.ForPaymentPaid(order);
                        await PublishSafelyAsync(notificationEvent);
                    }
                }

                return Ok(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
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
    }
}
