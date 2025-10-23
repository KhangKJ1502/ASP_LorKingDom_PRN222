using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BlogRepository : IBlogRepository
    {
        private readonly AspLorKingDomContext _context;

        public BlogRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<BlogPost>> GetAllAsync()
        {
            var blogs = await _context.BlogPosts
                .Where(b => !b.IsDeleted)
                .Include(b => b.Account)
                .Include(b => b.BlogCategories)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            //Console.WriteLine($"Database query returned {blogs.Count} blogs");
            return blogs;
        }

        public async Task<BlogPost?> GetByIdAsync(int id)
        {
            return await _context.BlogPosts
                .Where(b => !b.IsDeleted)
                .Include(b => b.Account)
                .Include(b => b.BlogCategories)
                .FirstOrDefaultAsync(b => b.BlogPostId == id);
        }

        public async Task<int> CreateAsync(BlogPost blog)
        {
            await _context.BlogPosts.AddAsync(blog);
            await _context.SaveChangesAsync();
            return blog.BlogPostId;
        }

        public async Task<bool> UpdateAsync(BlogPost blog)
        {
            _context.BlogPosts.Update(blog);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetFeaturedBlogCountAsync()
        {
            return await _context.BlogPosts
                .Where(b => !b.IsDeleted && b.IsFeatured)
                .CountAsync();
        }
    }
}
