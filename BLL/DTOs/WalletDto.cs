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
}