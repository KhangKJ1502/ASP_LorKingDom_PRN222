using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 9)
        {
            var blogs = await _blogService.GetAllAsync(q);

            // Filter chỉ blog đã xuất bản
            var publishedBlogs = blogs.Where(b => b.IsPublished).OrderByDescending(b => b.CreatedAt).ToList();

            // Separate featured and recent blogs
            var featuredBlogs = publishedBlogs.Where(b => b.IsFeatured).Take(4).ToList();
            var recentBlogs = publishedBlogs.Where(b => !b.IsFeatured).ToList();

            // Pagination cho recent posts
            var totalCount = recentBlogs.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedRecentBlogs = recentBlogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.FeaturedBlogs = featuredBlogs;
            ViewBag.RecentBlogs = pagedRecentBlogs;
            ViewBag.Query = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

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
        public async Task<IActionResult> ManageBlog(string? q)
        {
            var blogs = await _blogService.GetAllAsync(q);
            ViewBag.Query = q;

            return View("~/Views/Admin/ManageBlog.cshtml", blogs);
        }

        [HttpGet("Blog/GetDetail/{id}")]
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
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryService.GetAllAsync();
            ViewBag.Categories = categories;
            return View("~/Views/Admin/CreateBlog.cshtml");
        }

        [HttpPost("Blog/Create")]
        //[Authorize(Roles = "Admin")]
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
        //[Authorize(Roles = "Admin")]
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
        //[Authorize(Roles = "Admin")]
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
        //[Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _blogService.SoftDeleteAsync(id);
                if (!success)
                    throw new InvalidOperationException("Không thể xóa bài viết.");

                TempData["Success"] = "Xóa bài viết thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Manage");
        }

        [HttpGet("BlogReview/Manage")]
        public IActionResult ManageBlogReview()
        {
            return View("~/Views/Admin/ManageBlogReview.cshtml");
        }


        // ==================== Comment Actions ====================

        [HttpGet("Blog/{id}/Reviews")]
        public async Task<IActionResult> GetBlogComments(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserId = userId != null ? int.Parse(userId) : (int?)null;

                var reviews = await _reviewBlogService.GetByBlogIdAsync(id, currentUserId);
                return PartialView("_CommentsList", reviews);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return PartialView("_CommentsList", new List<ReviewBlogDto>());
            }
        }

        [HttpPost("Blog/{id}/Reviews")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int id, ReviewBlogDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return RedirectToAction("Detail", new { id });

                var accountId = int.Parse(userId);

                // Check if user already commented
                if (!await _reviewBlogService.CanCommentAsync(id, accountId))
                    return BadRequest("Bạn đã bình luận rồi. Mỗi người chỉ được bình luận 1 lần trên mỗi bài viết.");

                dto.BlogPostId = id;
                dto.AccountId = accountId;
                dto.Rating = 5; // Default rating

                var reviewId = await _reviewBlogService.CreateAsync(dto);

                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Blog/Review/{reviewId}/React")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactToReview(int reviewId, string reactionType)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return RedirectToReferrer();

                var accountId = int.Parse(userId);

                if (reactionType != "like" && reactionType != "dislike")
                    return BadRequest("Reaction type không hợp lệ");

                var review = await _reviewBlogService.GetByIdAsync(reviewId, accountId);
                if (review == null)
                    return BadRequest("Bình luận không tồn tại");

                await _reactionService.AddOrUpdateReactionAsync(reviewId, accountId, reactionType);

                return RedirectToAction("Detail", new { id = review.BlogPostId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Blog/Review/{reviewId}/Reply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToReview(int reviewId, ReviewBlogReplyDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return RedirectToReferrer();

                var accountId = int.Parse(userId);
                var user = await _accountService.GetByIdAsync(accountId);

                // Only admin/moderator can reply
                if (user?.RoleId != 1 && user?.RoleId != 2)
                    return BadRequest("Chỉ quản lý mới có thể phản hồi bình luận");

                var review = await _reviewBlogService.GetByIdAsync(reviewId, accountId);
                if (review == null)
                    return BadRequest("Bình luận không tồn tại");

                dto.ReviewBlogId = reviewId;
                dto.AccountId = accountId;

                await _replyService.CreateAsync(dto);

                return RedirectToAction("Detail", new { id = review.BlogPostId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private IActionResult RedirectToReferrer()
        {
            return Redirect(Request.Headers["Referer"].ToString() ?? "/Blog/Index");
        }
    }
}