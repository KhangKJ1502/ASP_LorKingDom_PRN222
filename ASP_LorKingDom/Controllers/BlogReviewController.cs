using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class BlogReviewController : Controller
    {
        private readonly IReviewBlogService _reviewBlogService;
        private readonly IBlogService _blogService;
        private readonly IReviewBlogReplyService _replyService;

        public BlogReviewController(
            IReviewBlogService reviewBlogService,
            IBlogService blogService,
            IReviewBlogReplyService replyService)
        {
            _reviewBlogService = reviewBlogService;
            _blogService = blogService;
            _replyService = replyService;
        }

        [HttpGet("Admin/BlogReview/Manage")]
        public async Task<IActionResult> ManageBlogReview(string? q, string? blog, string? user, int page = 1, int pageSize = 10)
        {
            try
            {
                // Get all reviews
                var allReviews = await _reviewBlogService.GetAllReviewsAsync();

                // Lọc theo search query (nội dung bình luận)
                if (!string.IsNullOrWhiteSpace(q))
                {
                    allReviews = allReviews
                        .Where(r => (r.Comment ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Lọc theo blog
                if (!string.IsNullOrWhiteSpace(blog) && int.TryParse(blog, out int blogId))
                {
                    allReviews = allReviews.Where(r => r.BlogPostId == blogId).ToList();
                }

                // Lọc theo người bình luận
                if (!string.IsNullOrWhiteSpace(user))
                {
                    allReviews = allReviews
                        .Where(r => (r.AuthorName ?? "").Contains(user, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Sắp xếp theo ngày mới nhất
                allReviews = allReviews.OrderByDescending(r => r.CreatedAt).ToList();

                // Pagination
                var totalCount = allReviews.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedReviews = allReviews
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.Query = q;
                ViewBag.BlogFilter = blog;
                ViewBag.UserFilter = user;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;

                // Load danh sách blog để dropdown
                var blogs = await _blogService.GetAllAsync();
                ViewBag.Blogs = blogs;

                return View("~/Views/Admin/ManageBlogReview.cshtml", pagedReviews);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("Admin/BlogReview/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                await _reviewBlogService.DeleteAsync(id);
                TempData["Success"] = "Xóa bình luận thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            // Get the blog id to redirect back
            var review = await _reviewBlogService.GetByIdAsync(id);
            if (review != null)
            {
                return RedirectToAction("ManageBlogReview", new { blog = review.BlogPostId });
            }

            return RedirectToAction("ManageBlogReview");
        }

        [HttpGet("Admin/BlogReview/Details/{id}")]
        public async Task<IActionResult> GetReviewDetail(int id)
        {
            try
            {
                var review = await _reviewBlogService.GetByIdAsync(id);
                if (review == null)
                    return NotFound();

                // Load replies
                var replies = await _replyService.GetByReviewIdAsync(id);

                var result = new
                {
                    reviewId = review.ReviewBlogId,
                    content = review.Comment,
                    rating = review.Rating,
                    authorName = review.AuthorName,
                    authorEmail = review.AuthorEmail,
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    likeCount = review.LikeCount,
                    dislikeCount = review.DislikeCount,
                    replies = replies?.Select(r => new
                    {
                        replyId = r.ReviewBlogReplyId,
                        content = r.Content,
                        authorName = r.AuthorName,
                        createdAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("Admin/BlogReview/Reply/{reviewId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToReview(int reviewId, string replyContent)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return BadRequest("Bạn cần đăng nhập");

                if (string.IsNullOrWhiteSpace(replyContent))
                    return BadRequest("Nội dung phản hồi không được để trống");

                var accountId = int.Parse(userId);

                var review = await _reviewBlogService.GetByIdAsync(reviewId);
                if (review == null)
                    return BadRequest("Bình luận không tồn tại");

                var dto = new ReviewBlogReplyDto
                {
                    ReviewBlogId = reviewId,
                    Content = replyContent,
                    AccountId = accountId
                };

                await _replyService.CreateAsync(dto);
                TempData["Success"] = "Phản hồi thành công!";

                return RedirectToAction("ManageBlogReview", new { blog = review.BlogPostId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("ManageBlogReview");
            }
        }

        [HttpPost("Admin/BlogReview/DeleteReply/{replyId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            try
            {
                var reply = await _replyService.GetByIdAsync(replyId);
                if (reply == null)
                    return BadRequest("Phản hồi không tồn tại");

                var reviewId = reply.ReviewBlogId;
                var review = await _reviewBlogService.GetByIdAsync(reviewId);

                await _replyService.DeleteAsync(replyId);
                TempData["Success"] = "Xóa phản hồi thành công!";

                return RedirectToAction("ManageBlogReview", new { blog = review?.BlogPostId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("ManageBlogReview");
            }
        }
    }
}
