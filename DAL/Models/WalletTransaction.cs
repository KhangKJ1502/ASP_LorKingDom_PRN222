using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class WalletTransaction
{
    public long WalletTransactionId { get; set; }

    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public string TxnType { get; set; } = null!;

    public string Direction { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    public int? RelatedOrderId { get; set; }

    public long? RelatedPaymentHistoryId { get; set; }

    public string? Method { get; set; }

    public string? ExternalRef { get; set; }

    public string? IdempotencyKey { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = new List<OrderRefund>();

    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

    public virtual Order? RelatedOrder { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
