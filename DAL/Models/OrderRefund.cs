namespace DAL.Models;

public partial class OrderRefund
{
    public long RefundId { get; set; }

    public int OrderId { get; set; }

    public int AccountId { get; set; }

    public int? RequestedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public string RefundMode { get; set; } = null!;

    public string RefundStatus { get; set; } = null!;

    public long? WalletTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Account? RequestedByNavigation { get; set; }

    public virtual WalletTransaction? WalletTransaction { get; set; }
}
