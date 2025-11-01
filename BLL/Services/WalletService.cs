using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;

        public WalletService(IWalletRepository walletRepo)
        {
            _walletRepo = walletRepo;
        }

        public async Task<WalletDto?> GetByAccountIdAsync(int accountId)
        {
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
            if (wallet == null) return null;

            return new WalletDto
            {
                WalletId = wallet.WalletId,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                Status = wallet.Status,
                LastTransactionAt = wallet.LastTransactionAt
            };
        }

        public async Task<int> CreateAsync(int accountId, string? walletName = null)
        {
            var exists = await _walletRepo.ExistsByAccountIdAsync(accountId);
            if (exists)
                throw new InvalidOperationException("Bạn đã có ví rồi!");

            var wallet = new Wallet
            {
                AccountId = accountId,
                Currency = "VND",
                Balance = 0,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            await _walletRepo.AddAsync(wallet);
            await _walletRepo.SaveChangesAsync();

            return wallet.WalletId;
        }
    }
}