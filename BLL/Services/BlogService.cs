using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class BlogService : IBlogService
    {
        private readonly IBlogRepository _blogRepo;
        private readonly IBlogCategoryRepository _categoryRepo;
        private const int MAX_FEATURED_BLOGS = 4;

        public BlogService(IBlogRepository blogRepo, IBlogCategoryRepository categoryRepo)
        {
            _blogRepo = blogRepo;
            _categoryRepo = categoryRepo;
        }

        public async Task<List<BlogPostDto>> GetAllAsync(string? keyword = null)
        {
            var blogs = await _blogRepo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                blogs = blogs.Where(b => b.BlogTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                        b.BlogContent.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var result = blogs.Select(Map).ToList();
            return result;
        }

        public async Task<BlogPostDto?> GetByIdAsync(int id)
        {
            var blog = await _blogRepo.GetByIdAsync(id);
            return blog == null ? null : Map(blog);
        }

        public async Task<int> GetFeaturedBlogCountAsync()
        {
            return await _blogRepo.GetFeaturedBlogCountAsync();
        }

        public async Task<bool> CanAddFeaturedBlogAsync(int? excludeBlogId = null)
        {
            var count = await GetFeaturedBlogCountAsync();
            // Nếu excludeBlogId được cung cấp, tức là đang edit một blog đã featured
            // nên không cần trừ đi số lượng hiện tại
            if (excludeBlogId.HasValue)
            {
                var blog = await _blogRepo.GetByIdAsync(excludeBlogId.Value);
                if (blog != null && blog.IsFeatured)
                {
                    // Blog đã featured rồi, cho phép chỉnh sửa
                    return true;
                }
            }
            return count < MAX_FEATURED_BLOGS;
        }

        public async Task<int> CreateAsync(BlogPostDto dto, int[] categoryIds)
        {
            // Kiểm tra nếu muốn đánh dấu featured
            if (dto.IsFeatured)
            {
                if (!await CanAddFeaturedBlogAsync())
                {
                    throw new InvalidOperationException($"Không thể thêm bài viết nổi bật. Giới hạn tối đa là {MAX_FEATURED_BLOGS} bài.");
                }
            }

            var blog = new BlogPost
            {
                AccountId = dto.AccountId,
                BlogTitle = dto.BlogTitle,
                BlogContent = dto.BlogContent,
                BlogThumbnail = dto.BlogThumbnail,
                BlogUrl = dto.BlogUrl ?? dto.BlogTitle.ToLower().Replace(" ", "-"),
                IsPublished = dto.IsPublished,
                IsFeatured = dto.IsFeatured,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            var id = await _blogRepo.CreateAsync(blog);

            // Thêm categories
            if (categoryIds.Length > 0)
            {
                foreach (var catId in categoryIds)
                {
                    var category = await _categoryRepo.GetByIdAsync(catId);
                    if (category != null && !category.IsDeleted)
                    {
                        blog.BlogCategories.Add(category);
                    }
                }
                await _blogRepo.UpdateAsync(blog);
            }

            return id;
        }

        public async Task<bool> UpdateAsync(int id, BlogPostDto dto, int[] categoryIds)
        {
            var blog = await _blogRepo.GetByIdAsync(id);
            if (blog == null) return false;

            // Kiểm tra nếu muốn đánh dấu featured nhưng hiện tại chưa featured
            if (dto.IsFeatured && !blog.IsFeatured)
            {
                if (!await CanAddFeaturedBlogAsync(id))
                {
                    throw new InvalidOperationException($"Không thể đánh dấu bài viết nổi bật. Giới hạn tối đa là {MAX_FEATURED_BLOGS} bài.");
                }
            }

            blog.BlogTitle = dto.BlogTitle;
            blog.BlogContent = dto.BlogContent;
            blog.BlogThumbnail = dto.BlogThumbnail;
            blog.IsPublished = dto.IsPublished;
            blog.IsFeatured = dto.IsFeatured;
            blog.UpdatedAt = DateTime.Now;

            // Cập nhật categories
            blog.BlogCategories.Clear();
            if (categoryIds.Length > 0)
            {
                foreach (var catId in categoryIds)
                {
                    var category = await _categoryRepo.GetByIdAsync(catId);
                    if (category != null && !category.IsDeleted)
                    {
                        blog.BlogCategories.Add(category);
                    }
                }
            }

            await _blogRepo.UpdateAsync(blog);
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var blog = await _blogRepo.GetByIdAsync(id);
            if (blog == null) return false;

            blog.IsDeleted = true;
            blog.UpdatedAt = DateTime.Now;
            await _blogRepo.UpdateAsync(blog);
            return true;
        }

        private static BlogPostDto Map(BlogPost blog) => new()
        {
            BlogPostId = blog.BlogPostId,
            AccountId = blog.AccountId,
            BlogTitle = blog.BlogTitle,
            BlogContent = blog.BlogContent,
            BlogThumbnail = blog.BlogThumbnail,
            BlogUrl = blog.BlogUrl,
            IsPublished = blog.IsPublished,
            IsFeatured = blog.IsFeatured,
            IsDeleted = blog.IsDeleted,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            AuthorName = blog.Account?.Email ?? "Unknown",
            CategoryIds = blog.BlogCategories?.Select(bc => bc.BlogCategoryId).ToList() ?? new List<int>(),
            CategoryNames = blog.BlogCategories?.Select(bc => bc.BlogCategoryName).ToList() ?? new List<string>()
        };
    }
}
