using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AspLorKingDomContext _db;

        public WalletRepository(AspLorKingDomContext db)
        {
            _db = db;
        }

        public async Task<Wallet?> GetByAccountIdAsync(int accountId)
        {
            return await _db.Wallets
                .FirstOrDefaultAsync(w => w.AccountId == accountId);
        }

        public async Task AddAsync(Wallet wallet)
        {
            await _db.Wallets.AddAsync(wallet);
        }

        public async Task<bool> ExistsByAccountIdAsync(int accountId)
        {
            return await _db.Wallets.AnyAsync(w => w.AccountId == accountId);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}