using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    public class ReviewProductController : Controller
    {
        private readonly IReviewProductService _reviewProductService;
        private readonly IProductService _productService;
        private readonly IReviewProductReplyService _replyService;
        private readonly IReviewProductReactionService _reactionService;
        private readonly IAccountService _accountService;

        public ReviewProductController(
            IReviewProductService reviewProductService,
            IProductService productService,
            IReviewProductReplyService replyService,
            IReviewProductReactionService reactionService,
            IAccountService accountService)
        {
            _reviewProductService = reviewProductService;
            _productService = productService;
            _replyService = replyService;
            _reactionService = reactionService;
            _accountService = accountService;
        }

        [HttpGet("ReviewProduct/Manage")]
        public async Task<IActionResult> ManageProductReview(string? q, string? product, string? deleteStatus, string? replyStatus, int page = 1, int pageSize = 10, bool showDeleted = false)
        {
            try
            {
                var allReviews = await _reviewProductService.GetAllReviewsAsync(showDeleted);

                // Search filter
                if (!string.IsNullOrWhiteSpace(q))
                {
                    allReviews = allReviews
                        .Where(r =>
                            (r.Comment ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            (r.AuthorName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            (r.ProductName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Product filter
                if (!string.IsNullOrWhiteSpace(product) && int.TryParse(product, out int productId))
                {
                    allReviews = allReviews.Where(r => r.ProductId == productId).ToList();
                }

                // Delete status filter
                if (!string.IsNullOrWhiteSpace(deleteStatus))
                {
                    bool isDeleted = deleteStatus == "deleted";
                    allReviews = allReviews.Where(r => r.IsDeleted == isDeleted).ToList();
                }

                // Reply status filter
                if (!string.IsNullOrWhiteSpace(replyStatus))
                {
                    bool hasReplies = replyStatus == "replied";
                    allReviews = allReviews
                        .Where(r => (r.Replies?.Count > 0) == hasReplies)
                        .ToList();
                }

                // Sort by latest
                allReviews = allReviews.OrderByDescending(r => r.CreatedAt).ToList();

                // Pagination
                var totalCount = allReviews.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedReviews = allReviews
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.Query = q;
                ViewBag.ProductFilter = product;
                ViewBag.DeleteStatusFilter = deleteStatus;
                ViewBag.ReplyStatusFilter = replyStatus;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.ShowDeleted = showDeleted;

                // Load products for dropdown
                var products = await _productService.GetAllAsync();
                ViewBag.Products = products;

                return View("~/Views/Admin/ManageProductReview.cshtml", pagedReviews);
            }
            catch
            {
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("ReviewProduct/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var success = await _reviewProductService.DeleteAsync(id);
                if (!success)
                    return BadRequest(new { error = "Failed to move review to recycle bin" });

                return Json(new { message = "Review moved to recycle bin" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("ReviewProduct/Restore/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreReview(int id)
        {
            try
            {
                var success = await _reviewProductService.RestoreAsync(id);
                if (!success)
                    return BadRequest(new { error = "Failed to restore review" });

                return Json(new { message = "Review restored successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("ReviewProduct/Details/{id}")]
        public async Task<IActionResult> GetReviewDetail(int id)
        {
            try
            {
                var review = await _reviewProductService.GetByIdAsync(id);
                if (review == null)
                    return NotFound();

                var replies = await _replyService.GetByReviewIdAsync(id);

                var result = new
                {
                    reviewId = review.ReviewProductId,
                    content = review.Comment,
                    rating = review.Rating,
                    authorName = review.AuthorName,
                    authorEmail = review.AuthorEmail,
                    productName = review.ProductName,
                    productId = review.ProductId,
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    isVerifiedPurchase = review.IsVerifiedPurchase,
                    isDeleted = review.IsDeleted,
                    likeCount = review.LikeCount,
                    dislikeCount = review.DislikeCount,
                    imageUrls = review.ImageUrls,
                    replies = replies?.Select(r => new
                    {
                        replyId = r.ReplyProductId,
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

        [HttpPost("ReviewProduct/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int id, ReviewProductDto dto)
        {
            try
            {
                var review = await _reviewProductService.GetByIdAsync(id);
                if (review == null)
                    return BadRequest(new { error = "Review not found" });

                var updateDto = new ReviewProductDto
                {
                    ReviewProductId = id,
                    ProductId = review.ProductId,
                    AccountId = review.AccountId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    IsVerifiedPurchase = dto.IsVerifiedPurchase,
                    IsDeleted = dto.IsDeleted
                };

                var success = await _reviewProductService.UpdateAsync(id, updateDto);
                if (!success)
                    return BadRequest(new { error = "Failed to update review" });

                return Json(new { message = "Review updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("ReviewProduct/Reply/{reviewId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToReview(int reviewId, [FromForm] string replyContent)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int accountId))
                    return Json(new { success = false, error = "Phiên đăng nhập hết hạn." });

                if (string.IsNullOrWhiteSpace(replyContent))
                    return Json(new { success = false, error = "Vui lòng nhập nội dung phản hồi." });

                var review = await _reviewProductService.GetByIdAsync(reviewId);
                if (review == null)
                    return Json(new { success = false, error = "Không tìm thấy đánh giá." });

                var dto = new ReviewProductReplyDto
                {
                    ReviewProductId = reviewId,
                    AccountId = accountId,
                    Content = replyContent.Trim()
                };

                await _replyService.CreateAsync(dto);
                return Json(new { success = true, message = "Phản hồi đã được gửi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("ReviewProduct/DeleteReply/{replyId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            try
            {
                var reply = await _replyService.GetByIdAsync(replyId);
                if (reply == null)
                    return BadRequest(new { error = "Reply not found" });

                await _replyService.DeleteAsync(replyId);
                return Json(new { message = "Reply deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}