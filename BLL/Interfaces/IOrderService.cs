using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IOrderService
    {
        Task<(bool success, string message, int? orderId)> CreateOrderAsync(int accountId, CheckoutDto checkoutDto);
        Task<List<OrderDto>> GetOrdersByAccountIdAsync(int accountId);
        Task<OrderDto?> GetOrderByIdAsync(int orderId);

        // Admin methods
        Task<PagedResult<OrderDto>> GetAllOrdersAsync(string? query, int? statusId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
        Task<bool> UpdateOrderStatusAsync(int orderId, int newStatusId);

        // Review Product 
        Task<List<PendingReviewDto>> GetPendingReviewProductsAsync(int accountId);
        Task<bool> CanReviewProductAsync(int accountId, int productId);
        Task MarkProductAsReviewedAsync(int accountId, int productId);
        Task UnmarkProductAsReviewedAsync(int accountId, int productId);
    }
}
