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
}