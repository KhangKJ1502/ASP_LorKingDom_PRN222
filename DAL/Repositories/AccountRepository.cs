using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AspLorKingDomContext _db;

        public AccountRepository(AspLorKingDomContext db)
        {
            _db = db;
        }

        // ===== Public lists (Active & !Deleted) =====
        public async Task<List<Account>> GetAllAsync() =>
            await _db.Accounts
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.Status == "Active")
                .ToListAsync();

        public async Task<List<Account>> GetAllCustomerAsync() =>
            await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == 4 && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();

        public async Task<List<Account>> GetAllStaffAsync() =>
            await _db.Accounts
                .AsNoTracking()
                .Where(a => (a.RoleId == 2 || a.RoleId == 3) && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();

        public async Task<List<Account>> GetByRoleIdAsync(int roleId) =>
            await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == roleId && !a.IsDeleted && a.Status == "Active")
                .ToListAsync();

        // ===== Admin lists (NO filter) =====
        public async Task<List<Account>> AdminGetAllStaffAsync() =>
            await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == 2 || a.RoleId == 3)
                .ToListAsync();

        public async Task<List<Account>> AdminGetAllCustomerAsync() =>
            await _db.Accounts
                .AsNoTracking()
                .Where(a => a.RoleId == 4)
                .ToListAsync();

        public async Task<Account?> GetByIdAsync(int accountId) =>
            await _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

        public async Task<Account?> GetByEmailAsync(string email) =>
            await _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == email);

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

        public async Task<bool> ExistsByEmailAsync(string email) =>
            await _db.Accounts.AnyAsync(x => x.Email == email);

        public async Task<bool> ExistsByPhoneAsync(string phoneNumber, int? excludeId = null)
        {
            var query = _db.Accounts.Where(a => a.PhoneNumber == phoneNumber);

            if (excludeId.HasValue)
                query = query.Where(a => a.AccountId != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}
