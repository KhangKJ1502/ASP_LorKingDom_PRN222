using DAL.Models;

namespace DAL.Interfaces
{
    public interface IVoucherRepository
    {
        Task<List<Voucher>> GetAllAsync();
        Task<Voucher?> GetByIdAsync(int voucherId);
        Task<Voucher?> GetByCodeAsync(string code);
        Task<int> CreateAsync(Voucher voucher);
        Task<bool> UpdateAsync(Voucher voucher);
        Task<bool> VoucherCodeExistsAsync(string voucherCode, int? excludeVoucherId = null);
        Task<bool> DeleteAsync(int voucherId);
        Task<int> CountByVoucherAndAccountAsync(int voucherId, int accountId);
    }
}