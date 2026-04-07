using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Payment
{
    public interface IPaymentService
    {
        /// Tạo link thanh toán
        Task<PaymentResponse> CreatePaymentLinkAsync(PaymentRequest request);

        /// Xác thực thanh toán
        Task<PaymentVerification> VerifyPaymentAsync(string orderId);

        /// Xử lý callback từ payment gateway
        Task<bool> HandleCallbackAsync(Dictionary<string, string> callbackData);

        /// Lấy tên provider
        string GetProviderName();
    }
}
