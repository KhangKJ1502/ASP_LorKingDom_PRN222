namespace BLL.Interfaces;
using BLL.DTOs;

public interface IAddressService
{
    Task<List<AddressDto>> GetByAccountIdAsync(int accountId);
    Task<AddressDto?> GetAsync(int id, int accountId);
    Task<int> AddAsync(int accountId, string addressLine, string city, string? ward, bool? setDefault);
    Task UpdateAsync(int id, int accountId, string addressLine, string city, string? ward, bool? setDefault);
    Task DeleteAsync(int id, int accountId);
    Task SetDefaultAsync(int id, int accountId);
}
