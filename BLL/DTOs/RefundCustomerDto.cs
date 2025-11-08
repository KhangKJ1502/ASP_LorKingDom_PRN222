using System;
using System.Collections.Generic;

namespace BLL.DTOs
{
    public class CreateRefundRequestDto
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RefundMode { get; set; } = "Wallet";
    }

    public class RefundRequestResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? RefundId { get; set; }
    }

    public class CustomerRefundStatusDto
    {
        public long RefundId { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string RefundStatus { get; set; } = string.Empty;
        public string RefundStatusDisplay { get; set; } = string.Empty;
        public string RefundMode { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string StatusBadgeClass { get; set; } = string.Empty;
        public List<RefundStepDto> Timeline { get; set; } = new();
    }
}