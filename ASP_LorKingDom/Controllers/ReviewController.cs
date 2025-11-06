using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
	[Authorize]
	public class ReviewController : Controller
	{
		private readonly IReviewProductService _reviewService;
		private readonly IOrderService _orderService;
		private readonly IProductService _productService;
		private readonly IWebHostEnvironment _env;
        private readonly IReviewProductReactionService _reactionService;

        public ReviewController(
			IReviewProductService reviewService,
			IOrderService orderService,
			IProductService productService,
			IWebHostEnvironment env,
            IReviewProductReactionService reactionService)
		{
			_reviewService = reviewService;
			_orderService = orderService;
			_productService = productService;
			_env = env;
            _reactionService = reactionService;
        }

		private int GetCurrentAccountId()
		{
			var accountIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return int.TryParse(accountIdStr, out var accountId) ? accountId : 0;
		}

		[HttpGet]
		public async Task<IActionResult> Partial()
		{
			try
			{
				var accountId = GetCurrentAccountId();
				if (accountId == 0)
				{
					return Unauthorized();
				}

				var pendingReviews = await _orderService.GetPendingReviewProductsAsync(accountId);
				var myReviews = await _reviewService.GetReviewsByAccountAsync(accountId);

				var model = new ReviewsViewModel
				{
					PendingReviews = pendingReviews ?? new List<PendingReviewDto>(),
					MyReviews = myReviews ?? new List<ReviewProductDto>()
				};

				return PartialView("_ReviewsPartial", model);
			}
			catch (Exception ex)
			{
				// Log error để debug
				Console.WriteLine($"Error in Review/Partial: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");

				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ReactToReview([FromBody] ReactRequest request)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0)
                return Json(new { success = false, error = "Vui lòng đăng nhập" });

            try
            {
                var success = await _reactionService.ReactAsync(
                    request.ReviewId,
                    accountId,
                    request.ReactionType
                );

                return Json(new { success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
		public async Task<IActionResult> GetReviewData(int productId)
		{
			try
			{
				var accountId = GetCurrentAccountId();
				if (accountId == 0)
				{
					return Json(new { success = false, error = "Vui lòng đăng nhập" });
				}

				var product = await _productService.GetByIdAsync(productId);
				if (product == null)
				{
					return Json(new { success = false, error = "Không tìm thấy sản phẩm" });
				}

				// Check if can review
				var canReview = await _orderService.CanReviewProductAsync(accountId, productId);
				if (!canReview)
				{
					return Json(new { success = false, error = "Bạn chưa mua sản phẩm này hoặc đã đánh giá rồi." });
				}

				return Json(new
				{
					success = true,
					productId = product.Id,
					productName = product.ProductName,
					mainImageUrl = product.MainImageUrl ?? "/images/placeholder.png"
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetReviewData: {ex.Message}");
				return Json(new { success = false, error = $"Lỗi: {ex.Message}" });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromForm] CreateReviewRequest request)
		{
			try
			{
				var accountId = GetCurrentAccountId();
				if (accountId == 0)
				{
					return Json(new { success = false, error = "Vui lòng đăng nhập" });
				}

				// Validate
				if (request.Rating < 1 || request.Rating > 5)
				{
					return Json(new { success = false, error = "Vui lòng chọn số sao từ 1-5." });
				}

				if (string.IsNullOrWhiteSpace(request.Comment))
				{
					return Json(new { success = false, error = "Vui lòng nhập nội dung đánh giá." });
				}

				// Check if can review
				var canReview = await _orderService.CanReviewProductAsync(accountId, request.ProductId);
				if (!canReview)
				{
					return Json(new { success = false, error = "Bạn không thể đánh giá sản phẩm này." });
				}

				// Check if already reviewed
				var hasReviewed = await _reviewService.HasUserReviewedAsync(request.ProductId, accountId);
				if (hasReviewed)
				{
					return Json(new { success = false, error = "Bạn đã đánh giá sản phẩm này rồi." });
				}

				// Create review
				var dto = new ReviewProductDto
				{
					ProductId = request.ProductId,
					AccountId = accountId,
					Rating = request.Rating,
					Comment = request.Comment.Trim(),
					IsVerifiedPurchase = true,
					ImageUrls = new List<string>()
				};

				// Handle image uploads
				if (request.Images != null && request.Images.Any())
				{
					var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "reviews");
					if (!Directory.Exists(uploadPath))
					{
						Directory.CreateDirectory(uploadPath);
					}

					foreach (var image in request.Images.Take(5)) // Max 5 images
					{
						if (image.Length > 0)
						{
							var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
							var filePath = Path.Combine(uploadPath, fileName);

							using (var stream = new FileStream(filePath, FileMode.Create))
							{
								await image.CopyToAsync(stream);
							}

							dto.ImageUrls.Add($"/uploads/reviews/{fileName}");
						}
					}
				}

				var reviewId = await _reviewService.CreateAsync(dto);

				// Mark product as reviewed in order
				await _orderService.MarkProductAsReviewedAsync(accountId, request.ProductId);

				return Json(new { success = true, message = "Đánh giá của bạn đã được gửi thành công!" });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in Create Review: {ex.Message}");
				return Json(new { success = false, error = $"Lỗi: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> ViewMyReview(int reviewId)
		{
			try
			{
				var accountId = GetCurrentAccountId();
				if (accountId == 0)
				{
					return Unauthorized();
				}

				var review = await _reviewService.GetByIdAsync(reviewId);
				if (review == null || review.AccountId != accountId)
				{
					return NotFound();
				}

				return PartialView("_ViewReviewPartial", review);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in ViewMyReview: {ex.Message}");
				return StatusCode(500);
			}
		}

        [HttpGet]
        public async Task<IActionResult> ViewReviewPopup(int productId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                if (accountId == 0) return Json(new { success = false, error = "Unauthorized" });

                var review = await _reviewService.GetReviewByProductAndAccountAsync(productId, accountId);
                if (review == null) return NotFound();

                return PartialView("_ViewReviewPopupPartial", review);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ViewReviewPopup: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int reviewId)
		{
			try
			{
				var accountId = GetCurrentAccountId(); 
				if (accountId == 0)
				{
					return Json(new { success = false, error = "Vui lòng đăng nhập" });
				}

				var review = await _reviewService.GetByIdAsync(reviewId);
				if (review == null || review.AccountId != accountId)
				{
					return Json(new { success = false, error = "Không tìm thấy đánh giá." });
				}

				await _reviewService.DeleteAsync(reviewId);

				// Unmark as reviewed in order
				await _orderService.UnmarkProductAsReviewedAsync(accountId, review.ProductId);

				return Json(new { success = true, message = "Đã xóa đánh giá." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in Delete Review: {ex.Message}");
				return Json(new { success = false, error = $"Lỗi: {ex.Message}" });
			}
		}
	}

    // class helper
    public class ReactRequest
    {
        public int ReviewId { get; set; }
        public string ReactionType { get; set; } = string.Empty;
    }
    public class CreateReviewRequest
	{
		public int ProductId { get; set; }
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;
		public List<IFormFile>? Images { get; set; }
	}
}