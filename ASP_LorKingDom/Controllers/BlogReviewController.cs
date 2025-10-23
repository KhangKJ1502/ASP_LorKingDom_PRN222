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
        private readonly IReviewBlogReactionService _reactionService;
        private readonly IAccountService _accountService;

        public BlogReviewController(
            IReviewBlogService reviewBlogService,
            IBlogService blogService,
            IReviewBlogReplyService replyService,
            IReviewBlogReactionService reactionService,
            IAccountService accountService)
        {
            _reviewBlogService = reviewBlogService;
            _blogService = blogService;
            _replyService = replyService;
            _reactionService = reactionService;
            _accountService = accountService;
        }

        [HttpGet("BlogReview/Manage")]
        public async Task<IActionResult> ManageBlogReview(string? q, string? blog, string? blockStatus, string? replyStatus, int page = 1, int pageSize = 10)
        {
            try
            {
                // Get all reviews
                var allReviews = await _reviewBlogService.GetAllReviewsAsync();

                // Lọc theo search query (người bình luận, bài viết, nội dung)
                if (!string.IsNullOrWhiteSpace(q))
                {
                    allReviews = allReviews
                        .Where(r =>
                            (r.Comment ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            (r.AuthorName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            (r.BlogTitle ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Lọc theo blog
                if (!string.IsNullOrWhiteSpace(blog) && int.TryParse(blog, out int blogId))
                {
                    allReviews = allReviews.Where(r => r.BlogPostId == blogId).ToList();
                }

                // Lọc theo trạng thái cấm
                if (!string.IsNullOrWhiteSpace(blockStatus))
                {
                    bool isBlocked = blockStatus == "blocked";
                    allReviews = allReviews.Where(r => r.IsBlocked == isBlocked).ToList();
                }

                // Lọc theo trạng thái phản hồi
                if (!string.IsNullOrWhiteSpace(replyStatus))
                {
                    bool hasReplies = replyStatus == "replied";
                    allReviews = allReviews
                        .Where(r => (r.Replies?.Count > 0) == hasReplies)
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
                ViewBag.BlockStatusFilter = blockStatus;
                ViewBag.ReplyStatusFilter = replyStatus;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;

                // Load danh sách blog để dropdown
                var blogs = await _blogService.GetAllAsync();
                ViewBag.Blogs = blogs;

                return View("~/Views/Admin/ManageBlogReview.cshtml", pagedReviews);
            }
            catch
            {
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("BlogReview/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                await _reviewBlogService.DeleteAsync(id);
            }
            catch
            {
                // Log error if needed
            }

            // Get the blog id to redirect back
            var review = await _reviewBlogService.GetByIdAsync(id);
            if (review != null)
            {
                return RedirectToAction("ManageBlogReview", new { blog = review.BlogPostId });
            }

            return RedirectToAction("ManageBlogReview");
        }

        [HttpGet("BlogReview/Details/{id}")]
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
                    blogTitle = review.BlogTitle,
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    likeCount = review.LikeCount,
                    dislikeCount = review.DislikeCount,
                    isBlocked = review.IsBlocked,
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

        [HttpPost("BlogReview/Reply/{reviewId}")]
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
                return RedirectToAction("ManageBlogReview", new { blog = review.BlogPostId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("BlogReview/DeleteReply/{replyId}")]
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
                return RedirectToAction("ManageBlogReview", new { blog = review?.BlogPostId });
            }
            catch
            {
                return RedirectToAction("ManageBlogReview");
            }
        }

        [HttpPost("BlogReview/ToggleBlock/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlockReview(int id)
        {
            try
            {
                var review = await _reviewBlogService.GetByIdAsync(id);
                if (review == null)
                    return RedirectToAction("ManageBlogReview");

                // Đảo ngược IsBlocked
                review.IsBlocked = !review.IsBlocked;
                await _reviewBlogService.UpdateAsync(id, new ReviewBlogDto
                {
                    BlogPostId = review.BlogPostId,
                    AccountId = review.AccountId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    IsBlocked = review.IsBlocked
                });

                return RedirectToAction("ManageBlogReview", new { blog = review.BlogPostId });
            }
            catch
            {
                return RedirectToAction("ManageBlogReview");
            }
        }

        // ==================== Public Review Actions (cho trang chi tiết blog) ====================

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
                    return RedirectToAction("Detail", "Blog", new { id });

                var accountId = int.Parse(userId);

                // Check if user already commented
                if (!await _reviewBlogService.CanCommentAsync(id, accountId))
                    return BadRequest("Bạn đã bình luận rồi. Mỗi người chỉ được bình luận 1 lần trên mỗi bài viết.");

                dto.BlogPostId = id;
                dto.AccountId = accountId;
                dto.Rating = 5; // Default rating

                var reviewId = await _reviewBlogService.CreateAsync(dto);

                return RedirectToAction("Detail", "Blog", new { id });
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

                return RedirectToAction("Detail", "Blog", new { id = review.BlogPostId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Blog/Review/{reviewId}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePublicReview(int reviewId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return BadRequest("Bạn cần đăng nhập");

                var accountId = int.Parse(userId);
                var review = await _reviewBlogService.GetByIdAsync(reviewId, accountId);

                if (review == null)
                    return BadRequest("Bình luận không tồn tại");

                // Chỉ chủ sở hữu bình luận hoặc admin mới được xoá
                var user = await _accountService.GetByIdAsync(accountId);
                if (review.AccountId != accountId && user?.RoleId != 1 && user?.RoleId != 2)
                    return BadRequest("Bạn không có quyền xoá bình luận này");

                await _reviewBlogService.DeleteAsync(reviewId);

                return RedirectToAction("Detail", "Blog", new { id = review.BlogPostId });
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
