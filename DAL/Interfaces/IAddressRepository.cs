using DAL.Models;

namespace DAL.Interfaces;
public interface IAddressRepository
{
    Task<List<Address>> ListAsync(int accountId);
    Task<Address?> GetAsync(int accountId, int addressId);
    Task AddAsync(Address entity);
    Task UpdateAsync(Address entity);
    Task DeleteAsync(Address entity);          
    Task<int> CountActiveAsync(int accountId);   
    Task<Address?> GetDefaultAsync(int accountId);
    Task<List<Address>> GetAllForAccountAsync(int accountId);
}
