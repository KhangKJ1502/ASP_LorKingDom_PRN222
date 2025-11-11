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


        /// Displays the blog listing page with filtering, search, and pagination.
        /// <param name="q">Search query for blog title or content</param>
        /// <param name="category">Category filter</param>
        /// <param name="page">Current page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 6)</param>
        /// <returns>View with filtered and paginated blog list</returns>
        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? category, int page = 1, int pageSize = 6)
        {
            // Get all blogs to calculate category statistics
            var allBlogs = await _blogService.GetAllAsync(null);
            var allPublishedBlogs = allBlogs
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.BlogPostId)
                .ToList();

            // Filter by search query if provided
            var filteredByQuery = allPublishedBlogs;
            if (!string.IsNullOrWhiteSpace(q))
            {
                filteredByQuery = allPublishedBlogs
                    .Where(b => (b.BlogTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (b.BlogContent?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            // Filter by category if provided
            var filteredBlogs = filteredByQuery;
            if (!string.IsNullOrWhiteSpace(category))
            {
                filteredBlogs = filteredByQuery
                    .Where(b => b.CategoryNames?.Contains(category, StringComparer.OrdinalIgnoreCase) ?? false)
                    .ToList();
            }

            // Separate featured and recent blogs
            var featuredBlogs = filteredBlogs.Where(b => b.IsFeatured).Take(4).ToList();
            var recentBlogs = filteredBlogs.Where(b => !b.IsFeatured).ToList();

            // Paginate recent blogs
            var totalCount = recentBlogs.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedRecentBlogs = recentBlogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Calculate category statistics from ALL published blogs (not filtered by category)
            var categoryStats = allPublishedBlogs
                .Where(b => b.CategoryNames != null)
                .SelectMany(b => b.CategoryNames)
                .Distinct()
                .ToDictionary(
                    cat => cat,
                    cat => allPublishedBlogs.Count(b => b.CategoryNames?.Contains(cat, StringComparer.OrdinalIgnoreCase) ?? false)
                );

            // Pass data to view via ViewBag
            ViewBag.FeaturedBlogs = featuredBlogs;
            ViewBag.RecentBlogs = pagedRecentBlogs;
            ViewBag.Query = q;
            ViewBag.Category = category;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.CategoryStats = categoryStats; // Dictionary containing count for each category

            return View(pagedRecentBlogs);
        }

        /// Displays detailed view of a single blog post.
        /// Shows related blogs from the same category and blog comments.
        /// <param name="id">Blog post ID</param>
        /// <returns>View with blog details, related blogs, and comments</returns>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var blog = await _blogService.GetByIdAsync(id);
            if (blog == null || !blog.IsPublished)
                return NotFound();

            // Get related blogs from the same category
            var allBlogs = await _blogService.GetAllAsync();
            var relatedBlogs = allBlogs
                .Where(b => b.IsPublished &&
                           b.BlogPostId != id &&
                           b.CategoryNames.Intersect(blog.CategoryNames).Any())
                .Take(3)
                .ToList();

            ViewBag.RelatedBlogs = relatedBlogs;

            // Load comments with user reaction information
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserId = userId != null ? int.Parse(userId) : (int?)null;
            var comments = await _reviewBlogService.GetByBlogIdAsync(id, currentUserId);
            ViewBag.Comments = comments;

            return View(blog);
        }


        /// Displays the admin blog management page with filtering and pagination.
        /// <param name="q">Search query for blog title or author name</param>
        /// <param name="category">Category filter</param>
        /// <param name="status">Publish status filter ("published" or "draft")</param>
        /// <param name="featured">Featured status filter</param>
        /// <param name="page">Current page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>Admin view with filtered and paginated blog list</returns>
        [HttpGet("Blog/Manage")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
        public async Task<IActionResult> ManageBlog(string? q, string? category, string? status, bool? featured, int page = 1, int pageSize = 10)
        {
            // Get all blogs without filtering at service layer
            var blogs = await _blogService.GetAllAsync(null);

            // Filter by search query (title or author name)
            if (!string.IsNullOrWhiteSpace(q))
            {
                blogs = blogs
                    .Where(b => (b.BlogTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                (b.AuthorName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            // Filter by category
            if (!string.IsNullOrWhiteSpace(category))
            {
                blogs = blogs
                    .Where(b => b.CategoryNames?.Contains(category, StringComparer.OrdinalIgnoreCase) ?? false)
                    .ToList();
            }

            // Filter by publish status
            if (!string.IsNullOrWhiteSpace(status))
            {
                bool isPublished = status == "published";
                blogs = blogs.Where(b => b.IsPublished == isPublished).ToList();
            }

            // Filter by featured status
            if (featured.HasValue)
            {
                blogs = blogs.Where(b => b.IsFeatured == featured.Value).ToList();
            }

            // Sort by creation date (newest first)
            blogs = blogs.OrderByDescending(b => b.CreatedAt).ToList();

            // Apply pagination
            var totalCount = blogs.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedBlogs = blogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Load category list for dropdown filter
            var categories = await _categoryService.GetAllAsync();
            var categoryNames = categories.Select(c => c.BlogCategoryName).Distinct().ToList();

            // Pass filter parameters and pagination data to view
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

        /// Retrieves detailed information of a blog post for admin viewing.
        /// Returns blog data in JSON format for modal display.
        /// <param name="id">Blog post ID</param>
        /// <returns>JSON object with blog details</returns>
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
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

        /// Displays the blog creation form.
        /// Loads available categories for selection.
        /// <returns>View with blog creation form</returns>
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryService.GetAllAsync();
            ViewBag.Categories = categories;
            return View("~/Views/Admin/CreateBlog.cshtml");
        }

        /// Creates a new blog post with the provided information.
        /// <param name="dto">Blog post data transfer object</param>
        /// <param name="categoryIds">Array of selected category IDs</param>
        /// <param name="thumbnail">Optional thumbnail image file</param>
        /// <returns>JSON result indicating success or failure</returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostDto dto, int[] categoryIds, IFormFile? thumbnail)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(dto.BlogTitle))
                    throw new ArgumentException("Tiêu đề blog không được để trống.");

                // Check featured blog limit before uploading image (max 4 featured blogs)
                if (dto.IsFeatured)
                {
                    if (!await _blogService.CanAddFeaturedBlogAsync())
                    {
                        return Json(new { success = false, message = "Không thể thêm blog nổi bật. Giới hạn tối đa là 4 blog nổi bật!" });
                    }
                }

                // Get current user account
                var user = await _accountService.GetByIdAsync(1); // TODO: Replace with actual current user
                if (user == null)
                    throw new InvalidOperationException("Không thể xác định người dùng hiện tại.");

                dto.AccountId = user.Id;

                // Upload thumbnail if provided
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

                // Create blog post
                var id = await _blogService.CreateAsync(dto, categoryIds);
                return Json(new { success = true, message = "Tạo bài viết thành công!" });
            }
            catch (Exception ex)
            {
                // Handle known exceptions with JSON response
                if (ex is InvalidOperationException)
                {
                    return Json(new { success = false, message = ex.Message });
                }

                // Show error on creation page
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                // Reload categories for form display
                var categories = await _categoryService.GetAllAsync();
                ViewBag.Categories = categories;
                return View("~/Views/Admin/CreateBlog.cshtml", dto);
            }
        }

        /// Displays the blog edit form with existing blog data.
        /// <param name="id">Blog post ID to edit</param>
        /// <returns>View with blog edit form</returns>
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _blogService.GetByIdAsync(id);
            if (blog == null)
            {
                return RedirectToAction("ManageBlog");
            }

            var categories = await _categoryService.GetAllAsync();
            ViewBag.Categories = categories;
            ViewBag.EditBlog = blog;
            return View("~/Views/Admin/EditBlog.cshtml", blog);
        }

        /// Updates an existing blog post with new information.
        /// <param name="id">Blog post ID to update</param>
        /// <param name="dto">Updated blog post data</param>
        /// <param name="categoryIds">Array of selected category IDs</param>
        /// <param name="thumbnail">Optional new thumbnail image file</param>
        /// <returns>JSON result indicating success or failure</returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostDto dto, int[] categoryIds, IFormFile? thumbnail)
        {
            try
            {
                Console.WriteLine($"Received IsFeatured: {dto.IsFeatured}");
                var blog = await _blogService.GetByIdAsync(id);
                if (blog == null)
                    throw new InvalidOperationException("Không tìm thấy bài viết!");

                // Check featured blog limit if changing from non-featured to featured
                if (dto.IsFeatured && !blog.IsFeatured)
                {
                    if (!await _blogService.CanAddFeaturedBlogAsync(id))
                    {
                        return Json(new { success = false, message = "Không thể đánh dấu nổi bật. Tối đa là 4 bài viết nổi bật." });
                    }
                }

                // Handle thumbnail upload or preserve existing
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
                    // Preserve existing thumbnail if no new upload
                    dto.BlogThumbnail = blog.BlogThumbnail;
                }

                // Preserve system fields
                dto.IsDeleted = blog.IsDeleted;
                dto.AccountId = blog.AccountId;

                // Update blog post
                await _blogService.UpdateAsync(id, dto, categoryIds);
                return Json(new { success = true, message = "Cập nhật bài viết thành công!" });
            }
            catch (Exception ex)
            {
                // Handle known exceptions with JSON response
                if (ex is InvalidOperationException)
                {
                    return Json(new { success = false, message = ex.Message });
                }

                // Show error on edit page
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                // Reload data for form display
                var categories = await _categoryService.GetAllAsync();
                ViewBag.Categories = categories;
                ViewBag.EditBlog = dto;
                return View("~/Views/Admin/EditBlog.cshtml", dto);
            }
        }

        /// Soft deletes a blog post.
        /// <param name="id">Blog post ID to delete</param>
        /// <returns>JSON result indicating success or failure</returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                var success = await _blogService.SoftDeleteAsync(id);
                if (!success)
                    return Json(new { success = false, message = "Không thể xoá bài viết!" });

                return Json(new { success = true, message = "Xoá bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
