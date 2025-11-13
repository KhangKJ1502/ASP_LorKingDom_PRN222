using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class OrderRefundService : IOrderRefundService
    {
        private readonly IOrderRefundRepository _repo;
        private readonly IOrderRepository _orderRepo;

        public OrderRefundService(IOrderRefundRepository repo, IOrderRepository orderRepo)
        {
            _repo = repo;
            _orderRepo = orderRepo;
        }

        // Search + paging cho trang Index
        public async Task<PagedResult<OrderRefundDto>> SearchRefundsAsync(
            string? q,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize)
        {
            var (data, total) = await _repo.SearchAsync(q, status, dateFrom, dateTo, page, pageSize);

            var mapped = data.Select(MapToDto).ToList();

            return new PagedResult<OrderRefundDto>
            {
                Items = mapped,
                Page = page,
                PageSize = pageSize,
                Total = total
            };
        }

        public async Task<OrderRefundDto?> GetByIdAsync(long refundId)
        {
            var entity = await _repo.GetByIdAsync(refundId);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<OrderRefundDetailDto?> GetDetailForModalAsync(long refundId)
        {
            var r = await _repo.GetByIdAsync(refundId);
            if (r == null) return null;

            var (displayText, _) = GetStatusPresentation(r.RefundStatus);

            // Tính final amount từ Order (nếu có)
            decimal orderFinalAmount = r.TotalAmount; // mặc định dùng TotalAmount
            if (r.Order != null)
            {
                orderFinalAmount = r.Order.PaidByWalletAmount + r.Order.PaidByExternalAmount;
            }

            // build steps timeline đơn giản dựa trên timestamps
            var steps = new List<RefundStepDto>();

            steps.Add(new RefundStepDto
            {
                Label = "Khách yêu cầu hoàn tiền",
                At = r.CreatedAt
            });

            if (r.ApprovedAt.HasValue && r.ApprovedBy.HasValue)
            {
                steps.Add(new RefundStepDto
                {
                    Label = r.RefundStatus == "Rejected"
                        ? "Nhân viên từ chối yêu cầu"
                        : "Nhân viên duyệt hoàn tiền",
                    At = r.ApprovedAt.Value
                });
            }

            if (r.ProcessedAt.HasValue && r.RefundStatus == "Refunded")
            {
                steps.Add(new RefundStepDto
                {
                    Label = "Hoàn tiền cho khách",
                    At = r.ProcessedAt.Value
                });
            }

            return new OrderRefundDetailDto
            {
                RefundId = r.RefundId,
                OrderId = r.OrderId,
                OrderCode = SafeOrderCode(r.Order),

                RequestedByName = r.RequestedByNavigation != null
                    ? SafeAccountName(r.RequestedByNavigation)
                    : null,

                ApprovedByName = r.ApprovedByNavigation != null
                    ? SafeAccountName(r.ApprovedByNavigation)
                    : null,

                RefundStatus = r.RefundStatus,
                RefundStatusDisplay = displayText,
                RefundMode = r.RefundMode,

                WalletTransactionId = r.WalletTransactionId,

                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                ProcessedAt = r.ProcessedAt,

                TotalAmount = orderFinalAmount, // Số tiền final của đơn hàng (đã tính sale, shipping, voucher)
                RefundAmount = r.RefundAmount,   // Số tiền hoàn (bằng final amount cho refund mới)
                Reason = r.Reason,

                Steps = steps
            };
        }

        public async Task ApproveOrUpdateStatusAsync(long refundId, string newStatus, int staffAccountId)
        {
            // Validate status
            var validStatuses = new[] { "Requested", "Approved", "Rejected", "Refunded", "Processing", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid status: {newStatus}");

            var current = await _repo.GetByIdAsync(refundId)
                ?? throw new KeyNotFoundException("Refund not found");

            // State machine validation
            if (current.RefundStatus == "Requested")
            {
                if (newStatus != "Approved" && newStatus != "Rejected")
                    throw new InvalidOperationException("Từ trạng thái 'Yêu cầu', chỉ có thể chuyển sang 'Duyệt' hoặc 'Từ chối'.");
            }
            else if (current.RefundStatus == "Approved")
            {
                if (newStatus != "Refunded" && newStatus != "Processing")
                    throw new InvalidOperationException("Từ trạng thái 'Đã duyệt', chỉ có thể chuyển sang 'Đang xử lý' hoặc 'Đã hoàn tiền'.");
            }
            else if (current.RefundStatus == "Processing")
            {
                if (newStatus != "Refunded")
                    throw new InvalidOperationException("Từ trạng thái 'Đang xử lý', chỉ có thể chuyển sang 'Đã hoàn tiền'.");
            }
            else
            {
                // Rejected, Refunded, Cancelled - không cho đổi
                throw new InvalidOperationException($"Yêu cầu hoàn tiền ở trạng thái '{current.RefundStatus}' không thể thay đổi.");
            }

            await _repo.UpdateStatusAsync(refundId, newStatus, staffAccountId);
        }

        // ========== helpers ==========

        private static OrderRefundDto MapToDto(OrderRefund r)
        {
            var requestedByName = r.RequestedByNavigation != null
                ? SafeAccountName(r.RequestedByNavigation)
                : null;

            var approvedByName = r.ApprovedByNavigation != null
                ? SafeAccountName(r.ApprovedByNavigation)
                : null;

            var orderCode = SafeOrderCode(r.Order);

            var (displayText, badgeCss) = GetStatusPresentation(r.RefundStatus);

            return new OrderRefundDto
            {
                RefundId = r.RefundId,
                OrderId = r.OrderId,
                OrderCode = orderCode,

                AccountId = r.AccountId,

                RequestedBy = r.RequestedBy,
                RequestedByName = requestedByName,

                ApprovedBy = r.ApprovedBy,
                ApprovedByName = approvedByName,

                RefundMode = r.RefundMode,
                RefundStatus = r.RefundStatus,

                WalletTransactionId = r.WalletTransactionId,

                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                ProcessedAt = r.ProcessedAt,
                UpdatedAt = r.UpdatedAt,

                RefundStatusDisplay = displayText,
                RefundStatusBadgeClass = badgeCss
            };
        }

        private static string SafeAccountName(Account acc)
        {
            if (acc == null)
                return "Unknown";

            if (!string.IsNullOrWhiteSpace(acc.AccountName))
                return acc.AccountName;

            if (!string.IsNullOrWhiteSpace(acc.Email))
                return acc.Email;

            return $"Account #{acc.AccountId}";
        }

        private static string? SafeOrderCode(Order? order)
        {
            if (order == null)
                return null;

            return $"Order #{order.OrderId}";
        }

        // Map trạng thái refund -> text tiếng Việt + CSS cho UI
        private static (string text, string badgeClass) GetStatusPresentation(string status)
        {
            return status switch
            {
                "Requested" => ("Yêu cầu hoàn tiền", "refund-badge refund-requested"),
                "Approved" => ("Đã duyệt", "refund-badge refund-approved"),
                "Rejected" => ("Từ chối", "refund-badge refund-rejected"),
                "Refunded" => ("Đã hoàn tiền", "refund-badge refund-refunded"),
                "Processing" => ("Đang xử lý", "refund-badge refund-processing"),
                "Cancelled" => ("Đã hủy", "refund-badge refund-cancelled"),
                _ => ($"Trạng thái: {status}", "refund-badge refund-unknown")
            };
        }

        public async Task<bool> CanRequestRefundAsync(int orderId, int accountId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null || order.AccountId != accountId)
                return false;

            if (order.StatusId != 4)
                return false;

            if (order.RefundStatus == "Requested" || order.RefundStatus == "Full")
                return false;

            var existingRefunds = await _repo.GetByOrderIdAsync(orderId);
            if (existingRefunds.Any(r => r.RefundStatus != "Rejected" && r.RefundStatus != "Cancelled"))
                return false;

            return true;
        }

        public async Task<RefundRequestResultDto> CreateRefundRequestAsync(int accountId, CreateRefundRequestDto dto)
        {
            if (dto.OrderId <= 0)
                return new RefundRequestResultDto { Success = false, Message = "OrderId không hợp lệ" };

            if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length < 10)
                return new RefundRequestResultDto { Success = false, Message = "Vui lòng nhập lý do ít nhất 10 ký tự" };

            var validModes = new[] { "Wallet", "OriginalPayment", "BankTransfer", "Cash" };
            if (!validModes.Contains(dto.RefundMode))
                return new RefundRequestResultDto { Success = false, Message = "Phương thức hoàn tiền không hợp lệ" };

            if (!await CanRequestRefundAsync(dto.OrderId, accountId))
                return new RefundRequestResultDto
                {
                    Success = false,
                    Message = "Đơn hàng này không thể yêu cầu hoàn tiền. Chỉ có thể hoàn tiền cho đơn hàng đã giao."
                };

            var order = await _orderRepo.GetByIdAsync(dto.OrderId);
            if (order == null)
                return new RefundRequestResultDto { Success = false, Message = "Không tìm thấy đơn hàng" };

            try
            {
                // Tính số tiền hoàn = số tiền user đã trả thực tế (bao gồm sale, shipping, voucher)
                var finalAmount = order.PaidByWalletAmount + order.PaidByExternalAmount;

                // Kiểm tra xem đã có refund trước đó chưa
                var existingRefunds = await _repo.GetByOrderIdAsync(dto.OrderId);

                // Kiểm tra có refund đã được approve/processed chưa
                var approvedRefund = existingRefunds
                    .FirstOrDefault(r => r.RefundStatus == "Approved" || r.RefundStatus == "Processed");

                if (approvedRefund != null)
                {
                    return new RefundRequestResultDto
                    {
                        Success = false,
                        Message = "Đơn hàng này đã được hoàn tiền. Không thể tạo yêu cầu hoàn tiền mới."
                    };
                }

                // Kiểm tra có refund đang pending chưa
                var pendingRefund = existingRefunds
                    .FirstOrDefault(r => r.RefundStatus == "Requested" || r.RefundStatus == "Pending");

                if (pendingRefund != null)
                {
                    return new RefundRequestResultDto
                    {
                        Success = false,
                        Message = "Đã có yêu cầu hoàn tiền đang chờ xử lý. Vui lòng đợi kết quả từ quản lý."
                    };
                }

                // Kiểm tra có refund bị rejected chưa - nếu có thì UPDATE thay vì INSERT
                var rejectedRefund = existingRefunds
                    .FirstOrDefault(r => r.RefundStatus == "Rejected");

                long refundId;

                if (rejectedRefund != null)
                {
                    // UPDATE refund cũ thay vì tạo mới
                    rejectedRefund.RefundMode = dto.RefundMode;
                    rejectedRefund.RefundStatus = "Requested";
                    rejectedRefund.TotalAmount = finalAmount;
                    rejectedRefund.RefundAmount = finalAmount;
                    rejectedRefund.Reason = dto.Reason;
                    rejectedRefund.UpdatedAt = DateTime.Now;
                    rejectedRefund.ApprovedBy = null;
                    rejectedRefund.ApprovedAt = null;
                    rejectedRefund.ProcessedAt = null;

                    await _repo.UpdateAsync(rejectedRefund);
                    refundId = rejectedRefund.RefundId;

                    return new RefundRequestResultDto
                    {
                        Success = true,
                        Message = $"Yêu cầu hoàn tiền đã được gửi lại thành công. Số tiền yêu cầu hoàn: {finalAmount:N0} ₫",
                        RefundId = refundId
                    };
                }
                else
                {
                    // Tạo refund mới (lần đầu tiên)
                    var refund = new OrderRefund
                    {
                        OrderId = dto.OrderId,
                        AccountId = accountId,
                        RequestedBy = accountId,
                        RefundMode = dto.RefundMode,
                        RefundStatus = "Requested",
                        TotalAmount = finalAmount,
                        RefundAmount = finalAmount,
                        Reason = dto.Reason,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    refundId = await _repo.CreateAsync(refund);

                    return new RefundRequestResultDto
                    {
                        Success = true,
                        Message = $"Yêu cầu hoàn tiền đã được gửi thành công. Số tiền yêu cầu hoàn: {finalAmount:N0} ₫",
                        RefundId = refundId
                    };
                }
            }
            catch (Exception ex)
            {
                return new RefundRequestResultDto
                {
                    Success = false,
                    Message = $"Có lỗi xảy ra: {ex.Message}"
                };
            }
        }

        public async Task<List<CustomerRefundStatusDto>> GetCustomerRefundHistoryAsync(int accountId)
        {
            var orders = await _orderRepo.GetByAccountIdAsync(accountId);
            var orderIds = orders.Select(o => o.OrderId).ToList();

            var allRefunds = new List<OrderRefund>();
            foreach (var orderId in orderIds)
            {
                var refunds = await _repo.GetByOrderIdAsync(orderId);
                allRefunds.AddRange(refunds);
            }

            return allRefunds
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapToCustomerDto)
                .ToList();
        }

        public async Task<CustomerRefundStatusDto?> GetCustomerRefundDetailAsync(int accountId, long refundId)
        {
            var refund = await _repo.GetByIdAsync(refundId);

            if (refund == null || refund.AccountId != accountId)
                return null;

            return MapToCustomerDto(refund);
        }

        // ========== Helper Methods ==========

        private static CustomerRefundStatusDto MapToCustomerDto(OrderRefund r)
        {
            var (displayText, badgeClass) = GetStatusPresentation(r.RefundStatus);

            var timeline = BuildTimeline(r);

            return new CustomerRefundStatusDto
            {
                RefundId = r.RefundId,
                OrderId = r.OrderId,
                OrderCode = SafeOrderCode(r.Order),
                RefundStatus = r.RefundStatus,
                RefundStatusDisplay = displayText,
                RefundMode = GetRefundModeDisplay(r.RefundMode),
                RefundAmount = r.RefundAmount,
                Reason = r.Reason,
                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                ProcessedAt = r.ProcessedAt,
                StatusBadgeClass = badgeClass,
                Timeline = timeline
            };
        }

        private static List<RefundStepDto> BuildTimeline(OrderRefund r)
        {
            var steps = new List<RefundStepDto>
    {
        new RefundStepDto { Label = "Gửi yêu cầu hoàn tiền", At = r.CreatedAt }
    };

            if (r.ApprovedAt.HasValue)
            {
                var label = r.RefundStatus == "Rejected"
                    ? "Yêu cầu bị từ chối"
                    : "Yêu cầu được chấp nhận";
                steps.Add(new RefundStepDto { Label = label, At = r.ApprovedAt.Value });
            }

            if (r.ProcessedAt.HasValue && r.RefundStatus == "Refunded")
            {
                steps.Add(new RefundStepDto { Label = "Hoàn tiền thành công", At = r.ProcessedAt.Value });
            }

            return steps;
        }

        private static string GetRefundModeDisplay(string mode)
        {
            return mode switch
            {
                "Wallet" => "💰 Ví LorKingDom",
                "OriginalPayment" => "💳 Phương thức thanh toán gốc",
                "BankTransfer" => "🏦 Chuyển khoản ngân hàng",
                "Cash" => "💵 Tiền mặt",
                _ => mode
            };
        }


    }
}