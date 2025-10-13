using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class PaymentHistory
{
    public long PaymentHistoryId { get; set; }

    public int OrderId { get; set; }

    public int AccountId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public long? WalletTransactionId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual WalletTransaction? WalletTransaction { get; set; }
}
