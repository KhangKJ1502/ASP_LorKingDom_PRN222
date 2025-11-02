using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.Filters;

namespace WebUI.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly IBlogCategoryService _categoryService;
        private readonly IAccountService _accountService;
        private readonly IReviewBlogService _reviewBlogService;
        private readonly IReviewBlogReactionService _reactionService;
        private readonly IReviewBlogReplyService _replyService;

        public BlogController(
            IBlogService blogService,
            IBlogCategoryService categoryService,
            IAccountService accountService,
            IReviewBlogService reviewBlogService,
            IReviewBlogReactionService reactionService,
            IReviewBlogReplyService replyService)
        {
            _blogService = blogService;
            _categoryService = categoryService;
            _accountService = accountService;
            _reviewBlogService = reviewBlogService;
            _reactionService = reactionService;
            _replyService = replyService;
        }

        // Public actions - Customer có thể xem blog
        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? category, int page = 1, int pageSize = 6)
        {
            // Lấy tất cả blogs để tính categoryStats
            var allBlogs = await _blogService.GetAllAsync(null);
            var allPublishedBlogs = allBlogs.Where(b => b.IsPublished).OrderByDescending(b => b.BlogPostId).ToList();

            // Lọc theo search query (nếu có)
            var filteredByQuery = allPublishedBlogs;
            if (!string.IsNullOrWhiteSpace(q))
            {
                filteredByQuery = allPublishedBlogs
                    .Where(b => (b.BlogTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (b.BlogContent?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            // Lọc theo chuyên mục (nếu có)
            var filteredBlogs = filteredByQuery;
            if (!string.IsNullOrWhiteSpace(category))
            {
                filteredBlogs = filteredByQuery
                    .Where(b => b.CategoryNames?.Contains(category, StringComparer.OrdinalIgnoreCase) ?? false)
                    .ToList();
            }

            // Tách biệt các blog nổi bật và gần đây
            var featuredBlogs = filteredBlogs.Where(b => b.IsFeatured).Take(4).ToList();
            var recentBlogs = filteredBlogs.Where(b => !b.IsFeatured).ToList();

            // Phân trang cho các bài đăng gần đây
            var totalCount = recentBlogs.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedRecentBlogs = recentBlogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Tính số lượng từng chuyên mục từ TẤT CẢ published blogs (không lọc category)
            var categoryStats = allPublishedBlogs
                .Where(b => b.CategoryNames != null)
                .SelectMany(b => b.CategoryNames)
                .Distinct()
                .ToDictionary(
                    cat => cat,
                    cat => allPublishedBlogs.Count(b => b.CategoryNames?.Contains(cat, StringComparer.OrdinalIgnoreCase) ?? false)
                );

            ViewBag.FeaturedBlogs = featuredBlogs;
            ViewBag.RecentBlogs = pagedRecentBlogs;
            ViewBag.Query = q;
            ViewBag.Category = category;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.CategoryStats = categoryStats; // Dictionary chứa count từng category

            return View(pagedRecentBlogs);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var blog = await _blogService.GetByIdAsync(id);
            if (blog == null || !blog.IsPublished)
                return NotFound();

            // Lấy các bài viết liên quan cùng category
            var allBlogs = await _blogService.GetAllAsync();
            var relatedBlogs = allBlogs
                .Where(b => b.IsPublished &&
                           b.BlogPostId != id &&
                           b.CategoryNames.Intersect(blog.CategoryNames).Any())
                .Take(3)
                .ToList();

            ViewBag.RelatedBlogs = relatedBlogs;

            // Load comments
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserId = userId != null ? int.Parse(userId) : (int?)null;
            var comments = await _reviewBlogService.GetByBlogIdAsync(id, currentUserId);
            ViewBag.Comments = comments;

            return View(blog);
        }

        [HttpGet("Blog/Manage")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        public async Task<IActionResult> ManageBlog(string? q, string? category, string? status, bool? featured, int page = 1, int pageSize = 10)
        {
            var blogs = await _blogService.GetAllAsync(null); // Lấy tất cả, không filter ở tầng service

            // Tìm kiếm theo tiêu đề hoặc tác giả
            if (!string.IsNullOrWhiteSpace(q))
            {
                blogs = blogs
                    .Where(b => (b.BlogTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                (b.AuthorName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            // Lọc theo chuyên mục
            if (!string.IsNullOrWhiteSpace(category))
            {
                blogs = blogs
                    .Where(b => b.CategoryNames?.Contains(category, StringComparer.OrdinalIgnoreCase) ?? false)
                    .ToList();
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                bool isPublished = status == "published";
                blogs = blogs.Where(b => b.IsPublished == isPublished).ToList();
            }

            // Lọc theo nổi bật
            if (featured.HasValue)
            {
                blogs = blogs.Where(b => b.IsFeatured == featured.Value).ToList();
            }

            // Sắp xếp
            blogs = blogs.OrderByDescending(b => b.CreatedAt).ToList();

            // Pagination
            var totalCount = blogs.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedBlogs = blogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Load danh sách chuyên mục cho dropdown
            var categories = await _categoryService.GetAllAsync();
            var categoryNames = categories.Select(c => c.BlogCategoryName).Distinct().ToList();

            ViewBag.Query = q;
            ViewBag.CategoryFilter = category;
            ViewBag.StatusFilter = status;
            ViewBag.FeaturedFilter = featured;
            ViewBag.Categories = categoryNames;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View("~/Views/Admin/ManageBlog.cshtml", pagedBlogs);
        }

        [HttpGet("Blog/GetDetail/{id}")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        public async Task<IActionResult> GetDetail(int id)
        {
            var blog = await _blogService.GetByIdAsync(id);
            if (blog == null)
                return NotFound();

            return Json(new
            {
                blogPostId = blog.BlogPostId,
                blogTitle = blog.BlogTitle,
                blogContent = blog.BlogContent,
                blogThumbnail = blog.BlogThumbnail,
                authorName = blog.AuthorName,
                createdAt = blog.CreatedAt,
                updatedAt = blog.UpdatedAt,
                categoryNames = blog.CategoryNames,
                isFeatured = blog.IsFeatured,
                isPublished = blog.IsPublished
            });
        }

        [HttpGet("Blog/Create")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryService.GetAllAsync();
            ViewBag.Categories = categories;
            return View("~/Views/Admin/CreateBlog.cshtml");
        }

        [HttpPost("Blog/Create")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostDto dto, int[] categoryIds, IFormFile? thumbnail)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(dto.BlogTitle))
                    throw new ArgumentException("Tiêu đề không được để trống.");

                // Kiểm tra giới hạn featured blogs trước khi upload ảnh
                if (dto.IsFeatured)
                {
                    if (!await _blogService.CanAddFeaturedBlogAsync())
                    {
                        return Json(new { success = false, message = "Không thể thêm bài viết nổi bật. Giới hạn tối đa là 4 bài." });
                    }
                }

                // Lấy user hiện tại
                //var user = await _accountService.GetCurrentUserAsync(User);
                var user = await _accountService.GetByIdAsync(1);
                if (user == null)
                    throw new InvalidOperationException("Không thể xác định người dùng hiện tại.");

                dto.AccountId = user.Id;

                // Upload thumbnail nếu có
                if (thumbnail != null)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(thumbnail.FileName);
                    var path = Path.Combine("wwwroot/uploads/blogs", fileName);
                    using (var stream = System.IO.File.Create(path))
                    {
                        await thumbnail.CopyToAsync(stream);
                    }
                    dto.BlogThumbnail = $"/uploads/blogs/{fileName}";
                }

                var id = await _blogService.CreateAsync(dto, categoryIds);
                TempData["Success"] = "Tạo bài viết thành công!";
                return Json(new { success = true, message = "Tạo bài viết thành công!" });
            }
            catch (Exception ex)
            {
                // Return JSON error
                if (ex is InvalidOperationException)
                {
                    return Json(new { success = false, message = ex.Message });
                }

                // ✅ Thêm: Hiển thị lỗi trên trang tạo blog
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                // Load lại categories để hiển thị trong form
                var categories = await _categoryService.GetAllAsync();
                ViewBag.Categories = categories;
                return View("~/Views/Admin/CreateBlog.cshtml", dto);
            }
        }

        [HttpGet("Blog/Edit/{id}")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _blogService.GetByIdAsync(id);
            if (blog == null)
            {
                TempData["Error"] = "Không tìm thấy bài viết.";
                return RedirectToAction("Manage");
            }

            var categories = await _categoryService.GetAllAsync();
            ViewBag.Categories = categories;
            ViewBag.EditBlog = blog;
            return View("~/Views/Admin/EditBlog.cshtml", blog);
        }

        [HttpPost("Blog/Edit/{id}")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostDto dto, int[] categoryIds, IFormFile? thumbnail)
        {
            try
            {
                Console.WriteLine($"Received IsFeatured: {dto.IsFeatured}");
                var blog = await _blogService.GetByIdAsync(id);
                if (blog == null)
                    throw new InvalidOperationException("Không tìm thấy bài viết.");

                // Kiểm tra giới hạn featured blogs nếu muốn chuyển từ không nổi bật sang nổi bật
                if (dto.IsFeatured && !blog.IsFeatured)
                {
                    if (!await _blogService.CanAddFeaturedBlogAsync(id))
                    {
                        return Json(new { success = false, message = "Không thể đánh dấu bài viết nổi bật. Giới hạn tối đa là 4 bài." });
                    }
                }

                if (thumbnail != null)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(thumbnail.FileName);
                    var path = Path.Combine("wwwroot/uploads/blogs", fileName);
                    using (var stream = System.IO.File.Create(path))
                    {
                        await thumbnail.CopyToAsync(stream);
                    }
                    dto.BlogThumbnail = $"/uploads/blogs/{fileName}";
                }
                else
                {
                    dto.BlogThumbnail = blog.BlogThumbnail;
                }

                dto.IsDeleted = blog.IsDeleted;
                dto.AccountId = blog.AccountId;

                await _blogService.UpdateAsync(id, dto, categoryIds);
                TempData["Success"] = "Cập nhật bài viết thành công!";
                return Json(new { success = true, message = "Cập nhật bài viết thành công!" });
            }
            catch (Exception ex)
            {
                // Return JSON error
                if (ex is InvalidOperationException)
                {
                    return Json(new { success = false, message = ex.Message });
                }

                // ✅ Thêm: Hiển thị lỗi trên trang edit blog
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                // Load lại data để hiển thị
                var categories = await _categoryService.GetAllAsync();
                ViewBag.Categories = categories;
                ViewBag.EditBlog = dto; // Hoặc load từ DB nếu cần
                return View("~/Views/Admin/EditBlog.cshtml", dto);
            }
        }

        [HttpPost("Blog/Delete/{id}")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Blog Management
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                var success = await _blogService.SoftDeleteAsync(id);
                if (!success)
                    return Json(new { success = false, message = "Không thể xóa bài viết." });

                return Json(new { success = true, message = "Xóa bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //[HttpGet("BlogReview/Manage")]
        //public IActionResult ManageBlogReview()
        //{
        //    return View("~/Views/Admin/ManageBlogReview.cshtml");
        //}
    }
}