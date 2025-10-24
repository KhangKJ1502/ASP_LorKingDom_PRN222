using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IVoucherRepository
    {
        Task<List<Voucher>> GetAllAsync();
        Task<Voucher?> GetByIdAsync(int voucherId);
        Task<int> CreateAsync(Voucher voucher);
        Task<bool> UpdateAsync(Voucher voucher);
        Task<bool> VoucherCodeExistsAsync(string voucherCode, int? excludeVoucherId = null);
    }
}