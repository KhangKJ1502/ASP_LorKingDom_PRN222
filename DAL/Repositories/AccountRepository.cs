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
            // Chỉ lấy account Active, chưa xoá (theo schema: IsDeleted BIT, Status IN ('Active',...))
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.Status == "Active")
                .ToListAsync();
        }

        public async Task<List<Account>> GetByRoleIdAsync(int roleId)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == roleId && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();
        }

        public Task<Account?> GetByIdAsync(int accountId)
        {
            return _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _db.Accounts.FirstOrDefaultAsync(x => x.Email == email);
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
