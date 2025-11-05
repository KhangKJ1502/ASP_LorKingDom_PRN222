using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly AspLorKingDomContext _db;

        public WalletTransactionRepository(AspLorKingDomContext db)
        {
            _db = db;
        }

        public async Task AddAsync(WalletTransaction transaction)
        {
            await _db.WalletTransactions.AddAsync(transaction);
        }

        public async Task<List<WalletTransaction>> GetByWalletIdAsync(int walletId, int pageSize = 20, int pageNumber = 1)
        {
            return await _db.WalletTransactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}