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

        // NEW: Customer functions
        Task<RefundRequestResultDto> CreateRefundRequestAsync(int accountId, CreateRefundRequestDto dto);
        Task<List<CustomerRefundStatusDto>> GetCustomerRefundHistoryAsync(int accountId);
        Task<CustomerRefundStatusDto?> GetCustomerRefundDetailAsync(int accountId, long refundId);
        Task<bool> CanRequestRefundAsync(int orderId, int accountId);
    }
}
