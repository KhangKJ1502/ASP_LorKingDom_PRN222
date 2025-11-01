using DAL.Models;

namespace DAL.Interfaces
{
    public interface IOrderRepository
    {
        Task<int> CreateOrderAsync(Order order);
        Task<int> CountByVoucherAndAccountAsync(int voucherId, int accountId);
        Task<List<Order>> GetOrdersByAccountIdAsync(int accountId);
        Task<Order?> GetOrderByIdAsync(int orderId);

        // Admin methods
        Task<(List<Order> orders, int totalCount)> GetAllOrdersAsync(string? query, int? statusId, DateTime? dateFrom, DateTime? dateTo, int skip, int take);
        Task<bool> UpdateOrderStatusAsync(int orderId, int newStatusId);
    }
}
