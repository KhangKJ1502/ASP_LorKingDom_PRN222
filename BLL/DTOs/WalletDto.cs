namespace BLL.DTOs
{
    public class WalletDto
    {
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = "Active";
        public DateTime? LastTransactionAt { get; set; }
    }

    public class CreateWalletDto
    {
        public string? WalletName { get; set; }
    }
    public class TopUpWalletDto
    {
        public decimal Amount { get; set; }
        public string? Method { get; set; } = "Manual"; // Manual, Momo, VNPay, etc.
        public string? Reason { get; set; }
    }

    public class TopUpResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewBalance { get; set; }
        public long? TransactionId { get; set; }
    }

    public class WalletTransactionDto
    {
        public long WalletTransactionId { get; set; }
        public string TxnType { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Method { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class TransactionHistoryDto
    {
        public List<WalletTransactionDto> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }      
        public int PageSize { get; set; }      

        public int TotalPages =>
            Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));

        public bool HasPrev => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}