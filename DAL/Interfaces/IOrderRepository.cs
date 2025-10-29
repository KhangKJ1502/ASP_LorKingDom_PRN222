using DAL.Models;

namespace DAL.Interfaces
{
    public interface IOrderRepository
    {
        Task<int> CreateOrderAsync(Order order);
        Task<int> CountByVoucherAndAccountAsync(int voucherId, int accountId);
    }
}
