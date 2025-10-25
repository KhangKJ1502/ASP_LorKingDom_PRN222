using System;

namespace BLL.DTOs
{
    public class VoucherDto
    {
        public int VoucherId { get; set; }
        public int VoucherTypeId { get; set; }
        public string? VoucherTypeName { get; set; }
        public int? CreateBy { get; set; }
        public string? CreatorName { get; set; }
        public string VoucherCode { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int? UsageLimitPerUser { get; set; }
        public bool IsStackable { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}