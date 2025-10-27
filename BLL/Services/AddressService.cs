using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;
public class AddressService : IAddressService
{
    private readonly IAddressRepository _repo;
    public AddressService(IAddressRepository repo) => _repo = repo;

    public async Task<List<AddressDto>> GetByAccountIdAsync(int accountId)
        => (await _repo.GetByAccountIdAsync(accountId))
            .Select(x => new AddressDto
            {
                AddressId = x.AddressId,
                AddressLine = x.AddressLine,
                City = x.City,
                Ward = x.Ward,
                IsDefault = x.IsDefault,
                CreatedAt = x.CreatedAt
            }).ToList();

    public async Task<AddressDto?> GetAsync(int id, int accountId)
    {
        var x = await _repo.GetByIdAsync(id, accountId);
        return x == null ? null : new AddressDto
        {
            AddressId = x.AddressId,
            AddressLine = x.AddressLine,
            City = x.City,
            Ward = x.Ward,
            IsDefault = x.IsDefault,
            CreatedAt = x.CreatedAt
        };
    }

    public async Task<int> AddAsync(int accountId, string addressLine, string city, string? ward, bool? setDefault)
    {
        var count = await _repo.CountByAccountAsync(accountId);
        var entity = new Address
        {
            AccountId = accountId,
            AddressLine = addressLine.Trim(),
            City = city.Trim(),
            Ward = string.IsNullOrWhiteSpace(ward) ? null : ward.Trim(),
            IsDefault = count == 0, // default khi là địa chỉ đầu tiên
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);

        // Nếu user tick setDefault và đã có sẵn địa chỉ khác
        if (setDefault == true && count > 0)
        {
            await _repo.SaveChangesAsync();                 // cần Id
            await _repo.SetDefaultAsync(entity.AddressId, accountId);
        }

        await _repo.SaveChangesAsync();
        return entity.AddressId;
    }

    public async Task UpdateAsync(int id, int accountId, string addressLine, string city, string? ward, bool? setDefault)
    {
        var entity = await _repo.GetByIdAsync(id, accountId) ?? throw new KeyNotFoundException("Address not found");
        entity.AddressLine = addressLine.Trim();
        entity.City = city.Trim();
        entity.Ward = string.IsNullOrWhiteSpace(ward) ? null : ward.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();

        if (setDefault == true)
        {
            await _repo.SetDefaultAsync(id, accountId);
            await _repo.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id, int accountId)
    {
        var willDelete = await _repo.GetByIdAsync(id, accountId) ?? throw new KeyNotFoundException("Address not found");
        bool deletingDefault = willDelete.IsDefault;

        await _repo.SoftDeleteAsync(id, accountId);
        await _repo.SaveChangesAsync();

        if (deletingDefault)
        {
            // nếu không còn default, đặt một cái khác làm default
            var remain = (await _repo.GetByAccountIdAsync(accountId)).FirstOrDefault();
            if (remain != null && !remain.IsDefault)
            {
                await _repo.SetDefaultAsync(remain.AddressId, accountId);
                await _repo.SaveChangesAsync();
            }
        }
    }

    public Task SetDefaultAsync(int id, int accountId) => _repo.SetDefaultAsync(id, accountId);
}
