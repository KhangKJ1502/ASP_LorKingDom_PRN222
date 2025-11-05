using DAL.Models;

namespace DAL.Interfaces
{
    public interface IWalletTransactionRepository
    {
        Task AddAsync(WalletTransaction transaction);
        Task<List<WalletTransaction>> GetByWalletIdAsync(int walletId, int pageSize = 20, int pageNumber = 1);
        Task SaveChangesAsync();
    }
}