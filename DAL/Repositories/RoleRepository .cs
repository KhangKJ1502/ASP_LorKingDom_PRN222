using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AspLorKingDomContext _db;
        public RoleRepository(AspLorKingDomContext db) => _db = db;

        public Task<Role?> GetByIdAsync(int roleId)
        {
            return _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == roleId);
        }

        public async Task<bool> ExistsAsync(int roleId)
        {
            return await _db.Roles
                .AsNoTracking()
                .AnyAsync(r => r.RoleId == roleId);
        }

        public Task<List<Role>> GetAllAsync()
        {
            return _db.Roles
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
