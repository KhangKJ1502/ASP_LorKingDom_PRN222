namespace BLL.Interfaces;
using BLL.DTOs;

public interface IAddressService
{
    Task<List<AddressDto>> ListAsync(int accountId);
    Task<AddressDto?> GetAsync(int accountId, int addressId);
    Task<int> CreateAsync(int accountId, string city, string ward, string addressLine, bool setAsDefault);
    Task<bool> UpdateAsync(int accountId, int addressId, string city, string ward, string addressLine, bool setAsDefault);
    Task<bool> DeleteAsync(int accountId, int addressId); 
    Task<bool> SetDefaultAsync(int accountId, int addressId);
}
