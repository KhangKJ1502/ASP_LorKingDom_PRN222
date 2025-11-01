using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly] // Staff: Blog Review Management
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
        public async Task<JsonResult> DeleteReview(int id)
        {
            try
            {
                var review = await _reviewBlogService.GetByIdAsync(id);
                if (review == null)
                    return Json(new { success = false, message = "Bình luận không tồn tại." });

                await _reviewBlogService.DeleteAsync(id);
                return Json(new { success = true, message = "Xóa bình luận thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
        public async Task<JsonResult> ReplyToReview(int reviewId, string replyContent)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Json(new { success = false, message = "Bạn cần đăng nhập để phản hồi." });

                if (string.IsNullOrWhiteSpace(replyContent))
                    return Json(new { success = false, message = "Nội dung phản hồi không được để trống." });

                var accountId = int.Parse(userId);

                var review = await _reviewBlogService.GetByIdAsync(reviewId);
                if (review == null)
                    return Json(new { success = false, message = "Bình luận không tồn tại." });

                var dto = new ReviewBlogReplyDto
                {
                    ReviewBlogId = reviewId,
                    Content = replyContent,
                    AccountId = accountId
                };

                await _replyService.CreateAsync(dto);
                return Json(new { success = true, message = "Phản hồi đã được gửi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
        public async Task<JsonResult> ToggleBlockReview(int id)
        {
            try
            {
                var review = await _reviewBlogService.GetByIdAsync(id);
                if (review == null)
                    return Json(new { success = false, message = "Bình luận không tồn tại." });

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

                var message = review.IsBlocked ? "Đã cấm bình luận!" : "Đã gỡ cấm bình luận!";
                return Json(new { success = true, message = message, isBlocked = review.IsBlocked });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
        public async Task<JsonResult> AddReview(int id, ReviewBlogDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận." });

                var accountId = int.Parse(userId);

                // Check if user already commented
                if (!await _reviewBlogService.CanCommentAsync(id, accountId))
                    return Json(new { success = false, message = "Bạn đã bình luận rồi. Mỗi người chỉ được bình luận 1 lần trên mỗi bài viết." });

                dto.BlogPostId = id;
                dto.AccountId = accountId;
                dto.Rating = 5; // Default rating

                var reviewId = await _reviewBlogService.CreateAsync(dto);

                return Json(new
                {
                    success = true,
                    message = "Bình luận của bạn đã được gửi thành công!",
                    reviewId = reviewId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Blog/Review/{reviewId}/React")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ReactToReview(int reviewId, string reactionType)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thả cảm xúc." });

                var accountId = int.Parse(userId);

                if (reactionType != "like" && reactionType != "dislike")
                    return Json(new { success = false, message = "Loại cảm xúc không hợp lệ." });

                var review = await _reviewBlogService.GetByIdAsync(reviewId, accountId);
                if (review == null)
                    return Json(new { success = false, message = "Bình luận không tồn tại." });

                await _reactionService.AddOrUpdateReactionAsync(reviewId, accountId, reactionType);

                // Get updated counts
                var updatedReview = await _reviewBlogService.GetByIdAsync(reviewId, accountId);

                return Json(new
                {
                    success = true,
                    message = "Đã cập nhật cảm xúc!",
                    likeCount = updatedReview?.LikeCount ?? 0,
                    dislikeCount = updatedReview?.DislikeCount ?? 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
