using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AspLorKingDomContext _context;

        public CategoryRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync(string? keyword)
        {
            var query = _context.Categories
                .Include(c => c.SuperCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.CategoryName.Contains(keyword));

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<List<Category>> GetActiveAsync()
        {
            return await _context.Categories
                .Include(c => c.SuperCategory)
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CategoryName)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.SuperCategory)
                .FirstOrDefaultAsync(x => x.CategoryId == id);
        }

        public async Task AddAsync(Category entity)
        {
            await _context.Categories.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category entity)
        {
            _context.Categories.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            return await _context.Categories.AnyAsync(x => x.CategoryName == name &&
                              (excludeId == null || x.CategoryId != excludeId));
        }

        public async Task<int> SetIsDeletedBySuperCategoryAsync(int superCategoryId, bool isDeleted)
        {
            // EF Core 7 có thể dùng ExecuteUpdateAsync, nhưng để tương thích rộng, update từng entity
            var items = await _context.Categories
                .Where(c => c.SuperCategoryId == superCategoryId)
                .ToListAsync();

            foreach (var c in items)
                c.IsDeleted = isDeleted;

            await _context.SaveChangesAsync();
            return items.Count;
        }
        public async Task<(List<Category> Items, int Total)> QueryPagedAsync(string? keyword, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = _context.Categories
                .Include(c => c.SuperCategory)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(x => x.CategoryName.Contains(keyword));

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.CategoryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

    }

}
