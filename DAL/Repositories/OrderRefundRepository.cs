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

        // Update trạng thái (ĐÃ SỬA lỗi FK WalletTransaction)
        public async Task UpdateStatusAsync(long refundId, string newStatus, int staffAccountId)
        {
            var refund = await _context.OrderRefunds
                .Include(r => r.Order)
                    .ThenInclude(o => o.Account) // Include Account để lấy ví
                .FirstOrDefaultAsync(r => r.RefundId == refundId);

            if (refund == null)
                throw new KeyNotFoundException($"Refund ID {refundId} not found.");

            // Validate status
            var validStatuses = new[] { "Requested", "Approved", "Rejected", "Refunded", "Cancelled", "Processing" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid status: {newStatus}");

            // Cập nhật status & timestamps chung
            refund.RefundStatus = newStatus;
            refund.UpdatedAt = DateTime.Now;

            // Nếu staff duyệt / từ chối
            if (newStatus == "Approved" || newStatus == "Rejected")
            {
                refund.ApprovedBy = staffAccountId;
                refund.ApprovedAt = DateTime.Now;
            }

            // ==========================
            //   TRƯỜNG HỢP REFUNDED
            // ==========================
            if (newStatus == "Refunded")
            {
                // Dùng transaction để đảm bảo thứ tự INSERT/UPDATE chuẩn,
                // tránh vi phạm FK_Refunds_WalletTxn khi gán WalletTransactionId
                using var tx = await _context.Database.BeginTransactionAsync();

                refund.ProcessedAt = DateTime.Now;

                // Cập nhật Order khi Refund xong
                if (refund.Order != null)
                {
                    var cancelledStatus = await _context.StatusOrders
                        .FirstOrDefaultAsync(s => s.StatusName == "Cancelled");

                    if (cancelledStatus != null)
                    {
                        refund.Order.StatusId = cancelledStatus.StatusId;
                        refund.Order.RefundStatus = "Full";
                        refund.Order.UpdatedAt = DateTime.Now;

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

                    // Hoàn vào ví nếu RefundMode = Wallet
                    if (refund.RefundMode == "Wallet")
                    {
                        var wallet = await _context.Wallets
                            .FirstOrDefaultAsync(w => w.AccountId == refund.AccountId);

                        if (wallet == null)
                            throw new InvalidOperationException($"Không tìm thấy ví của khách hàng AccountId: {refund.AccountId}");

                        var amount = refund.RefundAmount;
                        if (amount <= 0m)
                            throw new InvalidOperationException("RefundAmount không hợp lệ (<= 0).");

                        var balanceBefore = wallet.Balance;
                        var balanceAfter = balanceBefore + amount;

                        // 1) Tạo giao dịch ví trước → Save để có WalletTransactionId thật
                        var walletTxn = new WalletTransaction
                        {
                            WalletId = wallet.WalletId,
                            AccountId = refund.AccountId,
                            TxnType = "Refund",
                            Direction = "CR",
                            Amount = amount,
                            BalanceBefore = balanceBefore,
                            BalanceAfter = balanceAfter,
                            RelatedOrderId = refund.OrderId,
                            Method = "Wallet",
                            Status = "Completed",
                            Reason = $"Hoàn tiền đơn hàng #{refund.OrderId}",
                            IdempotencyKey = $"REFUND_{refundId}_{DateTime.Now.Ticks}",
                            CreatedAt = DateTime.Now,
                            CompletedAt = DateTime.Now
                        };

                        _context.WalletTransactions.Add(walletTxn);

                        // Cập nhật số dư ví cùng phase
                        wallet.Balance = balanceAfter;
                        wallet.LastTransactionAt = DateTime.Now;
                        wallet.UpdatedAt = DateTime.Now;

                        // 👉 Save lần 1: INSERT WalletTransactions + update Wallet
                        await _context.SaveChangesAsync();

                        // 2) Gán FK trên Refund sau khi có ID thật
                        refund.WalletTransactionId = walletTxn.WalletTransactionId;
                        refund.UpdatedAt = DateTime.Now;

                        // 👉 Save lần 2: UPDATE Refund (gán FK) + các thay đổi còn lại
                        await _context.SaveChangesAsync();

                        await tx.CommitAsync();
                        return; // tránh SaveChangesAsync() lần nữa ở cuối method
                    }
                }

                // Trường hợp RefundMode != "Wallet": chỉ cập nhật ProcessedAt/Order... → Save một lần
                await _context.SaveChangesAsync();
                return;
            }

            // ==========================
            //  Các trạng thái khác
            // ==========================

            // Cập nhật Order: Khi reject refund request
            if (newStatus == "Rejected" && refund.Order != null)
            {
                refund.Order.RefundStatus = "Rejected";
                refund.Order.UpdatedAt = DateTime.Now;
            }

            // Cập nhật Order: Khi approve/processing
            if ((newStatus == "Approved" || newStatus == "Processing") && refund.Order != null)
            {
                // Có yêu cầu hoàn/đang xử lý → giữ trạng thái "Requested"
                refund.Order.RefundStatus = "Requested";
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

            // ✅ CẬP NHẬT ORDER: Khi tạo refund request → Update Order.RefundStatus = "Requested"
            var order = await _context.Orders.FindAsync(refund.OrderId);
            if (order != null)
            {
                order.RefundStatus = "Requested"; // Có yêu cầu hoàn tiền đang chờ xử lý
                order.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return refund.RefundId;
        }

        // Get OrderRefunds by OrderId
        public async Task<IList<OrderRefund>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderRefunds
                .Include(r => r.Account)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.Order)
                .Where(r => r.OrderId == orderId)
                .AsNoTracking()
                .ToListAsync();
        }

        // Update OrderRefund entity
        public async Task UpdateAsync(OrderRefund refund)
        {
            if (refund == null)
                throw new ArgumentNullException(nameof(refund));

            // Re-query entity to avoid tracking conflicts
            var tracked = await _context.OrderRefunds
                .FirstOrDefaultAsync(r => r.RefundId == refund.RefundId);

            if (tracked == null)
                throw new InvalidOperationException($"OrderRefund {refund.RefundId} not found");

            // Update properties
            tracked.RefundStatus = refund.RefundStatus;
            tracked.RequestedBy = refund.RequestedBy;
            tracked.RefundMode = refund.RefundMode;
            tracked.ApprovedBy = refund.ApprovedBy;
            tracked.ApprovedAt = refund.ApprovedAt;
            tracked.ProcessedAt = refund.ProcessedAt;
            tracked.WalletTransactionId = refund.WalletTransactionId;
            tracked.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}
