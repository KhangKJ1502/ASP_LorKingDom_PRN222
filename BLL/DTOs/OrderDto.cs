namespace BLL.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public int AccountId { get; set; }
        public string? AccountName { get; set; }
        public int? VoucherId { get; set; }
        public string? VoucherCode { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddressLine { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingWard { get; set; } = string.Empty;
        public string? ShippingMethod { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidByWalletAmount { get; set; }
        public decimal PaidByExternalAmount { get; set; }
        public string RefundStatus { get; set; } = "None";
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin OrderRefund (nếu có)
        public string? OrderRefundStatus { get; set; } // Requested, Approved, Rejected, Refunded...
        public long? OrderRefundId { get; set; }

        public List<OrderDetailDto> OrderDetails { get; set; } = new();
    }

    public class OrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? MainImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public bool Reviewed { get; set; }
        public bool IsDeleted { get; set; }
    }
}
