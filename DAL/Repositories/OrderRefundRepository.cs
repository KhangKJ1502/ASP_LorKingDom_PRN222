using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class OrderRefundRepository : IOrderRefundRepository
    {
        private readonly AspLorKingDomContext _context;

        public OrderRefundRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        // Query có filter + paging
        public async Task<(IList<OrderRefund> data, int total)> SearchAsync(
            string? q,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize)
        {
            // Validate paging parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.OrderRefunds
                .Include(r => r.Account)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.Order)
                .AsNoTracking()
                .AsQueryable();

            // filter text: RefundId / OrderId / RequestedByName
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                query = query.Where(r =>
                    r.RefundId.ToString().Contains(kw) ||
                    r.OrderId.ToString().Contains(kw) ||
                    (r.RequestedByNavigation != null &&
                        (
                            (r.RequestedByNavigation.AccountName ?? "").ToLower().Contains(kw) ||
                            (r.RequestedByNavigation.Email ?? "").ToLower().Contains(kw)
                        )
                    )
                );
            }

            // status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.RefundStatus == status);
            }

            // date range theo CreatedAt
            if (dateFrom.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                // inclusive tới cuối ngày
                var end = dateTo.Value.Date.AddDays(1);
                query = query.Where(r => r.CreatedAt < end);
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }

        // Chi tiết 1 refund
        public async Task<OrderRefund?> GetByIdAsync(long refundId)
        {
            return await _context.OrderRefunds
                .Include(r => r.Account)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.Order)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RefundId == refundId);
        }

        // Update trạng thái
        public async Task UpdateStatusAsync(long refundId, string newStatus, int staffAccountId)
        {
            var refund = await _context.OrderRefunds
                .Include(r => r.Order) // Include Order để update
                .FirstOrDefaultAsync(r => r.RefundId == refundId);

            if (refund == null)
                throw new KeyNotFoundException($"Refund ID {refundId} not found.");

            // Validate status
            var validStatuses = new[] { "Requested", "Approved", "Rejected", "Refunded", "Cancelled", "Processing" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid status: {newStatus}");

            // cập nhật status
            refund.RefundStatus = newStatus;
            refund.UpdatedAt = DateTime.Now;

            // nếu staff duyệt
            if (newStatus == "Approved" || newStatus == "Rejected")
            {
                refund.ApprovedBy = staffAccountId;
                refund.ApprovedAt = DateTime.Now;
            }

            // nếu hoàn tất hoàn tiền → Cập nhật Order RefundStatus và StatusId
            if (newStatus == "Refunded")
            {
                refund.ProcessedAt = DateTime.Now;
                
                // ✅ CẬP NHẬT ORDER: Hoàn tiền thành công → Cancelled Order
                if (refund.Order != null)
                {
                    // Tìm StatusId của "Cancelled" trong bảng StatusOrder
                    var cancelledStatus = await _context.StatusOrders
                        .FirstOrDefaultAsync(s => s.StatusName == "Cancelled");
                    
                    if (cancelledStatus != null)
                    {
                        refund.Order.StatusId = cancelledStatus.StatusId;
                        refund.Order.RefundStatus = "Refunded";
                        refund.Order.UpdatedAt = DateTime.Now;
                        
                        // Tạo OrderStatusHistory để tracking
                        var statusHistory = new OrderStatusHistory
                        {
                            OrderId = refund.OrderId,
                            StatusId = cancelledStatus.StatusId,
                            ChangedAt = DateTime.Now,
                            ChangedBy = staffAccountId,
                            Note = $"Đơn hàng bị hủy do hoàn tiền thành công (RefundId: {refundId})",
                            CreatedAt = DateTime.Now
                        };
                        _context.OrderStatusHistories.Add(statusHistory);
                    }
                }
            }
            
            // ✅ CẬP NHẬT ORDER: Khi approve refund request
            if (newStatus == "Approved" && refund.Order != null)
            {
                refund.Order.RefundStatus = "Approved";
                refund.Order.UpdatedAt = DateTime.Now;
            }
            
            // ✅ CẬP NHẬT ORDER: Khi reject refund request
            if (newStatus == "Rejected" && refund.Order != null)
            {
                refund.Order.RefundStatus = "Rejected";
                refund.Order.UpdatedAt = DateTime.Now;
            }
            
            // ✅ CẬP NHẬT ORDER: Khi processing refund
            if (newStatus == "Processing" && refund.Order != null)
            {
                refund.Order.RefundStatus = "Processing";
                refund.Order.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // Tạo yêu cầu mới
        public async Task<long> CreateAsync(OrderRefund refund)
        {
            // Validate input
            if (refund == null)
                throw new ArgumentNullException(nameof(refund));

            if (refund.OrderId <= 0)
                throw new ArgumentException("OrderId must be greater than 0", nameof(refund));

            if (refund.AccountId <= 0)
                throw new ArgumentException("AccountId must be greater than 0", nameof(refund));

            if (refund.RefundAmount <= 0)
                throw new ArgumentException("RefundAmount must be greater than 0", nameof(refund));

            if (string.IsNullOrWhiteSpace(refund.RefundMode))
                throw new ArgumentException("RefundMode is required", nameof(refund));

            refund.CreatedAt = DateTime.Now;
            refund.UpdatedAt = DateTime.Now;
            refund.RefundStatus = "Requested";

            _context.OrderRefunds.Add(refund);
            await _context.SaveChangesAsync();

            return refund.RefundId;
        }
    }
}