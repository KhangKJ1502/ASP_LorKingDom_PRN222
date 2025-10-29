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

        public OrderRefundService(IOrderRefundRepository repo)
        {
            _repo = repo;
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

                TotalAmount = r.TotalAmount,
                RefundAmount = r.RefundAmount,
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

        public async Task<long> CreateRefundRequestAsync(
            int orderId,
            int accountId,
            int requestedByAccountId,
            string refundMode)
        {
            // Validate inputs
            if (orderId <= 0)
                throw new ArgumentException("OrderId must be greater than 0", nameof(orderId));

            if (accountId <= 0)
                throw new ArgumentException("AccountId must be greater than 0", nameof(accountId));

            if (requestedByAccountId <= 0)
                throw new ArgumentException("RequestedByAccountId must be greater than 0", nameof(requestedByAccountId));

            if (string.IsNullOrWhiteSpace(refundMode))
                throw new ArgumentException("RefundMode is required", nameof(refundMode));

            var entity = new OrderRefund
            {
                OrderId = orderId,
                AccountId = accountId,
                RequestedBy = requestedByAccountId,
                ApprovedBy = null,
                RefundMode = refundMode,
                RefundStatus = "Requested",
                WalletTransactionId = null,
                CreatedAt = DateTime.Now,
                ApprovedAt = null,
                ProcessedAt = null,
                UpdatedAt = DateTime.Now
            };

            var newId = await _repo.CreateAsync(entity);
            return newId;
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
    }
}