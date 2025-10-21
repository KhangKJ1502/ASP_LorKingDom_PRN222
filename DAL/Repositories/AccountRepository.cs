using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AspLorKingDomContext _db;
        public AccountRepository(AspLorKingDomContext db) => _db = db;

        public async Task<List<Account>> GetAllAsync()
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.Status == "Active")
                .ToListAsync();
        }

        public async Task<List<Account>> GetAllCustomerAsync()
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == 4 && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();
        }

        public async Task<List<Account>> GetAllStaffAsync()
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => (a.RoleId == 2 || a.RoleId == 3) && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();
        }

        public async Task<List<Account>> GetByRoleIdAsync(int roleId)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == roleId && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();
        }

        public async Task<Account?> GetByIdAsync(int accountId)
        {
            return await _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task AddAsync(Account entity)
        {
            await _db.Accounts.AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Account entity)
        {
            _db.Accounts.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _db.Accounts.AnyAsync(x => x.Email == email);
        }
    }
}
