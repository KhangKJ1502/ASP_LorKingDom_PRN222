using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IVoucherService
    {
        Task<List<VoucherDto>> GetAllVouchersAsync(bool includeDeleted = false);
        Task<VoucherDto?> GetByIdAsync(int voucherId);
        Task<int> CreateAsync(VoucherDto dto);
        Task<bool> UpdateAsync(int id, VoucherDto dto);
        Task<bool> DeleteAsync(int id);
    }
}