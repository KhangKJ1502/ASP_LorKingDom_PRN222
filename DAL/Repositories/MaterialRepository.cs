using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class MaterialRepository : IMaterialRepository
    {
        private readonly AspLorKingDomContext _context;

        public MaterialRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Material>> GetAllAsync(string? keyword)
        {
            var query = _context.Materials.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.MaterialName.Contains(keyword));

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Material>> GetActiveAsync()
        {
            return await _context.Materials
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.MaterialName)
                .ToListAsync();
        }

        public async Task<Material?> GetByIdAsync(int id)
        {
            return await _context.Materials.FirstOrDefaultAsync(x => x.MaterialId == id);
        }

        public async Task AddAsync(Material entity)
        {
            await _context.Materials.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Material entity)
        {
            _context.Materials.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            return await _context.Materials.AnyAsync(x => x.MaterialName == name &&
                              (excludeId == null || x.MaterialId != excludeId));
        }
    }
}
