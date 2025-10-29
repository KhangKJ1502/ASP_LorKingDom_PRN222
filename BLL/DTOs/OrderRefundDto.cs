using System;
using System.Collections.Generic;

namespace BLL.DTOs
{
    public class OrderRefundDto
    {
        public long RefundId { get; set; }

        public int OrderId { get; set; }
        public string? OrderCode { get; set; } // mã đơn hiển thị

        public int AccountId { get; set; } // chủ đơn hàng

        public int? RequestedBy { get; set; }
        public string? RequestedByName { get; set; }

        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }

        public string RefundMode { get; set; } = string.Empty;   // Wallet / OriginalPayment ...
        public string RefundStatus { get; set; } = string.Empty; // Requested / Approved / Rejected / Refunded / Processing

        public long? WalletTransactionId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // UI helper
        public string RefundStatusDisplay { get; set; } = string.Empty;        // ví dụ: "Yêu cầu hoàn tiền"
        public string RefundStatusBadgeClass { get; set; } = string.Empty;     // ví dụ: "refund-badge refund-requested"
    }

    // item dùng cho modal timeline từng bước
    public class RefundStepDto
    {
        public string Label { get; set; } = string.Empty;
        public DateTime At { get; set; }
    }

    // Chi tiết cho modal xem 1 refund
    public class OrderRefundDetailDto
    {
        public long RefundId { get; set; }

        public int OrderId { get; set; }
        public string? OrderCode { get; set; }

        public string? RequestedByName { get; set; }
        public string? ApprovedByName { get; set; }

        public string RefundStatus { get; set; } = string.Empty;
        public string RefundStatusDisplay { get; set; } = string.Empty;
        public string RefundMode { get; set; } = string.Empty;

        public long? WalletTransactionId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public decimal? TotalAmount { get; set; }
        public decimal? RefundAmount { get; set; }

        public string? Reason { get; set; }

        public List<RefundStepDto> Steps { get; set; } = new();
    }

}
