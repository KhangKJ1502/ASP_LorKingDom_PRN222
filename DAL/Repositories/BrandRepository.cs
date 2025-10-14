using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BrandRepository : IBrandRepository
    {
        private readonly AspLorKingDomContext _context;

        public BrandRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Brand>> GetAllAsync(string? keyword)
        {
            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.BrandName.Contains(keyword));

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<List<Brand>> GetActiveAsync()
        {
            return await _context.Brands
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.BrandName)
                .ToListAsync();
        }

        public async Task<Brand?> GetByIdAsync(int id)
        {
            return await _context.Brands.FirstOrDefaultAsync(x => x.BrandId == id);
        }

        public async Task AddAsync(Brand entity)
        {
            await _context.Brands.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Brand entity)
        {
            _context.Brands.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            return await _context.Brands.AnyAsync(x => x.BrandName == name &&
                              (excludeId == null || x.BrandId != excludeId));
        }
    }
}
