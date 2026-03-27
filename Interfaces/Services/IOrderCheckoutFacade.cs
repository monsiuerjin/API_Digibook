using API_DigiBook.Models;
using API_DigiBook.Notifications.Models;
using API_DigiBook.Commands;
using API_DigiBook.Interfaces.Commands;

namespace API_DigiBook.Interfaces.Services
{
    public interface IOrderCheckoutFacade
    {
        /// <summary>
        /// Xử lý quy trình thanh toán trọn gói (Facade)
        /// </summary>
        /// <param name="order">Thông tin đơn hàng</param>
        /// <returns>Kết quả thực thi (kèm CheckoutUrl nếu có)</returns>
        Task<CommandResult> CheckoutAsync(Order order);
    }
}
