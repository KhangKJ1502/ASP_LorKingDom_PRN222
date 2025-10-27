using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;
public class AddressRepository : IAddressRepository
{
    private readonly AspLorKingDomContext _ctx;
    public AddressRepository(AspLorKingDomContext ctx) => _ctx = ctx;

    public Task<List<Address>> GetByAccountIdAsync(int accountId) =>
        _ctx.Addresses
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

    public Task<Address?> GetByIdAsync(int id, int accountId) =>
        _ctx.Addresses.FirstOrDefaultAsync(x => x.AddressId == id && x.AccountId == accountId && !x.IsDeleted);

    public Task<int> CountByAccountAsync(int accountId) =>
        _ctx.Addresses.CountAsync(x => x.AccountId == accountId && !x.IsDeleted);

    public async Task AddAsync(Address entity)
    {
        await _ctx.Addresses.AddAsync(entity);
    }

    public Task UpdateAsync(Address entity)
    {
        _ctx.Addresses.Update(entity);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(int id, int accountId)
    {
        var a = await GetByIdAsync(id, accountId);
        if (a == null) return;
        a.IsDeleted = true;
        a.UpdatedAt = DateTime.UtcNow;
        _ctx.Addresses.Update(a);
    }

    public async Task ClearDefaultAsync(int accountId)
    {
        await _ctx.Addresses
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsDefault)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsDefault, false));
    }

    public async Task SetDefaultAsync(int id, int accountId)
    {
        await ClearDefaultAsync(accountId);
        await _ctx.Addresses
            .Where(x => x.AccountId == accountId && x.AddressId == id && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsDefault, true));
    }

    public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
}
