using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IOrderService
    {
        Task<(bool success, string message, int? orderId)> CreateOrderAsync(int accountId, CheckoutDto checkoutDto);
    }
}
