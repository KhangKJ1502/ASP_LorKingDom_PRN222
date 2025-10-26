using DAL.Interfaces;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;

namespace DAL.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AspLorKingDomContext _context;

        public AddressRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Address>> GetByAccountIdAsync(int accountId)
        {
            return await _context.Addresses
                .Where(a => a.AccountId == accountId && !a.IsDeleted)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
        }

        public async Task<Address?> GetByIdAsync(int id)
        {
            return await _context.Addresses.FindAsync(id);
        }

        public async Task AddAsync(Address address)
        {
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Address address)
        {
            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var addr = await _context.Addresses.FindAsync(id);
            if (addr != null)
            {
                addr.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
