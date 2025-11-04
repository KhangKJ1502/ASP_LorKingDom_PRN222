using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IOrderRefundService
    {
        Task<PagedResult<OrderRefundDto>> SearchRefundsAsync(
            string? q,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize
        );

        Task<OrderRefundDto?> GetByIdAsync(long refundId);

        Task<OrderRefundDetailDto?> GetDetailForModalAsync(long refundId);

        Task ApproveOrUpdateStatusAsync(long refundId, string newStatus, int staffAccountId);
    }
}
