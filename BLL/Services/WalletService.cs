using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletTransactionRepository _transactionRepo;

        // Constants cho transaction
        private const string DIRECTION_CREDIT = "CR";  // Tiền vào
        private const string DIRECTION_DEBIT = "DR";   // Tiền ra
        private const string TXN_TYPE_TOPUP = "TopUp";
        private const string TXN_TYPE_PAYMENT = "Payment";
        private const string TXN_TYPE_REFUND = "Refund";
        private const string METHOD_WALLET = "Wallet";
        private const string METHOD_CASH = "Cash";
        private const string METHOD_BANK = "Bank";
        private const string METHOD_EWALLET = "EWallet";

        public WalletService(
            IWalletRepository walletRepo,
            IWalletTransactionRepository transactionRepo)
        {
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
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


        public async Task<TransactionHistoryDto> GetTransactionHistoryAsync(int accountId, int page = 1, int pageSize = 10)
        {
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);

            if (wallet == null)
            {
                return new TransactionHistoryDto
                {
                    Transactions = new List<WalletTransactionDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            var transactions = await _transactionRepo.GetByWalletIdAsync(wallet.WalletId, pageSize, page);
            var totalCount = await _transactionRepo.GetTotalCountByWalletIdAsync(wallet.WalletId);

            return new TransactionHistoryDto
            {
                Transactions = transactions.Select(t => new WalletTransactionDto
                {
                    WalletTransactionId = t.WalletTransactionId,
                    TxnType = t.TxnType,
                    Direction = t.Direction,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Method = t.Method,
                    Status = t.Status,
                    Reason = t.Reason,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt
                }).ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<TopUpResultDto> TopUpAsync(int accountId, TopUpWalletDto dto)
        {
            // Validate amount
            if (dto.Amount <= 0)
            {
                return new TopUpResultDto
                {
                    Success = false,
                    Message = "Số tiền nạp phải lớn hơn 0"
                };
            }

            if (dto.Amount > 100_000_000)
            {
                return new TopUpResultDto
                {
                    Success = false,
                    Message = "Số tiền nạp không được vượt quá 100,000,000 VND"
                };
            }

            // Get wallet
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
            if (wallet == null)
            {
                return new TopUpResultDto
                {
                    Success = false,
                    Message = "Không tìm thấy ví"
                };
            }

            if (wallet.Status != "Active")
            {
                return new TopUpResultDto
                {
                    Success = false,
                    Message = "Ví của bạn đang bị khóa"
                };
            }

            try
            {
                // Create transaction
                var balanceBefore = wallet.Balance;
                var balanceAfter = balanceBefore + dto.Amount;

                var transaction = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    AccountId = accountId,
                    TxnType = TXN_TYPE_TOPUP,
                    Direction = DIRECTION_CREDIT, // CR = Credit = Tiền vào
                    Amount = dto.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Method = null, // Top-up thủ công không cần method cụ thể
                    Status = "Completed",
                    Reason = dto.Reason ?? "Nạp tiền vào ví",
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.Now,
                    CompletedAt = DateTime.Now
                };

                // Add transaction to context (KHÔNG save ngay)
                await _transactionRepo.AddAsync(transaction);

                // Update wallet balance
                wallet.Balance = balanceAfter;
                wallet.LastTransactionAt = DateTime.Now;
                wallet.UpdatedAt = DateTime.Now;
                await _walletRepo.UpdateAsync(wallet);

                await _walletRepo.SaveChangesAsync();

                return new TopUpResultDto
                {
                    Success = true,
                    Message = "Nạp tiền thành công",
                    NewBalance = balanceAfter,
                    TransactionId = transaction.WalletTransactionId
                };
            }
            catch (Exception ex)
            {
                // Log chi tiết để debug
                Console.WriteLine($"❌ TopUp Error: {ex.Message}");
                Console.WriteLine($"❌ Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");

                return new TopUpResultDto
                {
                    Success = false,
                    Message = $"Có lỗi xảy ra: {ex.InnerException?.Message ?? ex.Message}"
                };
            }
        }


    }
}