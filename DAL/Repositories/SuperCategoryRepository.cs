using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class SuperCategoryRepository : ISuperCategoryRepository
    {
        private readonly AspLorKingDomContext _context;

        public SuperCategoryRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<SuperCategory>> GetAllAsync(string? keyword)
        {
            var query = _context.SuperCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.SuperCategoryName.Contains(keyword));

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<List<SuperCategory>> GetActiveAsync()
        {
            return await _context.SuperCategories
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SuperCategoryName)
                .ToListAsync();
        }

        public async Task<List<SuperCategory>> SearchAsync(string keyword, bool includeDeleted = false)
        {
            var query = _context.SuperCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.SuperCategoryName.Contains(keyword));

            if (!includeDeleted)
                query = query.Where(x => !x.IsDeleted);

            return await query.OrderBy(x => x.SuperCategoryName).ToListAsync();
        }

        public async Task<SuperCategory?> GetByIdAsync(int id)
        {
            return await _context.SuperCategories.FirstOrDefaultAsync(x => x.SuperCategoryId == id);
        }

        public async Task AddAsync(SuperCategory entity)
        {
            await _context.SuperCategories.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

    

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            return await _context.SuperCategories.AnyAsync(x => x.SuperCategoryName == name &&
                              (excludeId == null || x.SuperCategoryId != excludeId));
        }

        public async Task UpdateAsync(SuperCategory entity)
        {
            _context.SuperCategories.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}

