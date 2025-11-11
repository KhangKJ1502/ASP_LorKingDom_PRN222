using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BlogCategoryRepository : IBlogCategoryRepository
    {
        private readonly AspLorKingDomContext _context;

        public BlogCategoryRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<BlogCategory>> GetAllAsync()
        {
            return await _context.BlogCategories
                .Where(bc => !bc.IsDeleted)
                .OrderByDescending(bc => bc.CreatedAt)
                .ToListAsync();
        }

        public async Task<BlogCategory?> GetByIdAsync(int id)
        {
            return await _context.BlogCategories
                .Include(bc => bc.BlogPosts) // Include để kiểm tra khi xóa
                .FirstOrDefaultAsync(bc => bc.BlogCategoryId == id);
        }

        public async Task<int> CreateAsync(BlogCategory category)
        {
            await _context.BlogCategories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category.BlogCategoryId;
        }

        public async Task<bool> UpdateAsync(BlogCategory category)
        {
            _context.BlogCategories.Update(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
