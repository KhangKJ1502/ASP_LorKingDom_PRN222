using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;
public class AddressRepository : IAddressRepository
{
    private readonly AspLorKingDomContext _context;
    public AddressRepository(AspLorKingDomContext context)
    {
        _context = context;
    }

    public async Task<List<Address>> ListAsync(int accountId, string? keyword)
    {
        var query = _context.Addresses
            .Where(a => a.AccountId == accountId); // ⬅️ bỏ lọc IsDeleted

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(a =>
                a.AddressLine.ToLower().Contains(kw) ||
                a.City.ToLower().Contains(kw) ||
                (a.Ward ?? string.Empty).ToLower().Contains(kw));
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Address?> GetAsync(int accountId, int addressId)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.AccountId == accountId && a.AddressId == addressId);
    }

    public async Task AddAsync(Address entity)
    {
        await _context.Addresses.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Address entity)
    {
        _context.Addresses.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Address entity) // ⬅️ xóa cứng
    {
        _context.Addresses.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountActiveAsync(int accountId)
    {
        return await _context.Addresses
            .CountAsync(a => a.AccountId == accountId); // ⬅️ không lọc IsDeleted
    }

    public async Task<Address?> GetDefaultAsync(int accountId)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.AccountId == accountId && a.IsDefault);
    }

    public async Task<List<Address>> GetAllForAccountAsync(int accountId)
    {
        return await _context.Addresses
            .Where(a => a.AccountId == accountId)
            .ToListAsync();
    }
}
