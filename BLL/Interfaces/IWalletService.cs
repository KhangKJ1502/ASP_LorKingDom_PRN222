using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IWalletService
    {
        Task<WalletDto?> GetByAccountIdAsync(int accountId);
        Task<int> CreateAsync(int accountId, string? walletName = null);
        Task<TopUpResultDto> TopUpAsync(int accountId, TopUpWalletDto dto);
    }
}