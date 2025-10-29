using DAL.Models;

namespace DAL.Interfaces
{
    public interface IOrderRefundRepository
    {
        Task<(IList<OrderRefund> data, int total)> SearchAsync(
            string? q,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize
        );

        Task<OrderRefund?> GetByIdAsync(long refundId);

        Task UpdateStatusAsync(long refundId, string newStatus, int staffAccountId);

        Task<long> CreateAsync(OrderRefund refund);
    }
}
