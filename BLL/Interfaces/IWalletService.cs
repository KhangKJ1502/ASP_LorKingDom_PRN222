using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IWalletService
    {
        Task<WalletDto?> GetByAccountIdAsync(int accountId);
        Task<int> CreateAsync(int accountId, string? walletName = null);
        Task<TopUpResultDto> TopUpAsync(int accountId, TopUpWalletDto dto);
        Task<TransactionHistoryDto> GetTransactionHistoryAsync(int accountId, int page = 1, int pageSize = 10);
    }
}