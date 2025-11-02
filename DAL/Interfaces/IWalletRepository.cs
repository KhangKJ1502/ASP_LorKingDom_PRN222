using DAL.Models;

namespace DAL.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByAccountIdAsync(int accountId);
        Task AddAsync(Wallet wallet);
        Task<bool> ExistsByAccountIdAsync(int accountId);
        Task SaveChangesAsync();
    }
}