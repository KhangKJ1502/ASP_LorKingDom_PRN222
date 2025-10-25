using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IAddressService
    {
        Task<List<AddressDto>> GetByAccountIdAsync(int accountId);
        Task AddAsync(int accountId, string addressLine, string city, string? ward, bool isDefault);
        Task UpdateAsync(int id, string addressLine, string city, string? ward, bool isDefault);
        Task DeleteAsync(int id);
    }
}
