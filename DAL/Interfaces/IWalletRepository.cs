using DAL.Models;

namespace DAL.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByAccountIdAsync(int accountId);
        Task<Wallet?> GetByIdAsync(int walletId);
        Task AddAsync(Wallet wallet);
        Task<bool> ExistsByAccountIdAsync(int accountId);
        Task UpdateAsync(Wallet wallet);
        Task SaveChangesAsync();
    }
}