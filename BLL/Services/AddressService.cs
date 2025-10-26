using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _repo;

        public AddressService(IAddressRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<AddressDto>> GetByAccountIdAsync(int accountId)
        {
            var list = await _repo.GetByAccountIdAsync(accountId);
            return list.Select(a => new AddressDto
            {
                AddressId = a.AddressId,
                AddressLine = a.AddressLine,
                City = a.City,
                Ward = a.Ward,
                IsDefault = a.IsDefault,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task AddAsync(int accountId, string addressLine, string city, string? ward, bool isDefault)
        {
            var addr = new Address
            {
                AccountId = accountId,
                AddressLine = addressLine,
                City = city,
                Ward = ward,
                IsDefault = isDefault,
                CreatedAt = DateTime.Now
            };
            await _repo.AddAsync(addr);
        }

        public async Task UpdateAsync(int id, string addressLine, string city, string? ward, bool isDefault)
        {
            var addr = await _repo.GetByIdAsync(id);
            if (addr == null) return;

            addr.AddressLine = addressLine;
            addr.City = city;
            addr.Ward = ward;
            addr.IsDefault = isDefault;
            addr.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(addr);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }
    }
}
