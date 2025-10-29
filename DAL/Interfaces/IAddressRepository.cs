using DAL.Models;

namespace DAL.Interfaces;
public interface IAddressRepository
{
    Task<List<Address>> ListAsync(int accountId, string? keyword);
    Task<Address?> GetAsync(int accountId, int addressId);
    Task AddAsync(Address entity);
    Task UpdateAsync(Address entity);
    Task DeleteAsync(Address entity);              // ⬅️ đổi từ SoftDelete → Delete (xóa cứng)
    Task<int> CountActiveAsync(int accountId);    // đếm tất cả địa chỉ hiện có (không dùng IsDeleted nữa)
    Task<Address?> GetDefaultAsync(int accountId);
    Task<List<Address>> GetAllForAccountAsync(int accountId);
}
