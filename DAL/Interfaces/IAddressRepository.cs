using DAL.Models;

namespace DAL.Interfaces;
public interface IAddressRepository
{
    Task<List<Address>> GetByAccountIdAsync(int accountId);
    Task<Address?> GetByIdAsync(int id, int accountId);
    Task<int> CountByAccountAsync(int accountId);
    Task AddAsync(Address entity);
    Task UpdateAsync(Address entity);
    Task SoftDeleteAsync(int id, int accountId);
    Task ClearDefaultAsync(int accountId);
    Task SetDefaultAsync(int id, int accountId);
    Task SaveChangesAsync();
}
