using BLL.DTOs;
using BLL.Interfaces;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace ASP_LorKingDom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productSvc;
        private readonly IWishlistService _wishlistSvc;
        private readonly IReviewProductService _reviewSvc;
        private readonly IAccountService _accountService;
        private readonly IAddressService _addressService;

        public HomeController(
            ILogger<HomeController> logger,
            IProductService productSvc,
            IWishlistService wishlistSvc,
            IAccountService accountService,
            IAddressService addressService,
            IReviewProductService reviewSvc)
        {
            _logger = logger;
            _productSvc = productSvc;
            _wishlistSvc = wishlistSvc;
            _accountService = accountService;
            _addressService = addressService;
            _reviewSvc = reviewSvc;
        }

        // ===== helpers =====
        private async Task<HashSet<int>> GetLikedSetAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return new();
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idStr, out var accountId)
                ? new HashSet<int>(await _wishlistSvc.GetProductIdsAsync(accountId))
                : new HashSet<int>();
        }

        private async Task<PagedResult<ProductDto>> BuildPagedModelAsync(string? q, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            var all = await _productSvc.GetAllAsync(q);

            var filtered = all.Where(p =>
                !p.IsDeleted &&
                p.StockQuantity > 0 &&
                p.ProductStatus == "Available" &&
                p.CategoryId != null &&
                p.MaterialId != null &&
                p.AgeId != null &&
                p.SexId != null &&
                p.PriceRangeId != null &&
                p.BrandId != null &&
                p.OriginId != null
            );

            var total = filtered.Count();

            var items = filtered
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var liked = await GetLikedSetAsync();
            foreach (var p in items) p.IsLiked = liked.Contains(p.Id);

            return new PagedResult<ProductDto>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 16)
        {
            var model = await BuildPagedModelAsync(q, page, pageSize);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ProductGrid(string? q, int page = 1, int pageSize = 16)
        {
            var model = await BuildPagedModelAsync(q, page, pageSize);
            return PartialView("_ProductGridPartial", model);
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            var dto = await _productSvc.GetByIdAsync(id);
            if (dto == null) return NotFound();

            var reviews = await _reviewSvc.GetReviewsByProductIdAsync(id);
            ViewBag.Reviews = reviews;

            var liked = await GetLikedSetAsync();
            dto.IsLiked = liked.Contains(id);

            return View(dto);
        }

        public IActionResult Privacy() => View();
        public IActionResult Contact() => View();
        public IActionResult Cart() => View();

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Challenge();

                var account = await _accountService.GetByIdAsync(int.Parse(userId));
                if (account == null)
                    return NotFound();

                return View(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile page");
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProfileOverview()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("ProfileOverview: No user ID found in claims");
                    return Unauthorized();
                }

                var account = await _accountService.GetByIdAsync(int.Parse(userId));
                if (account == null)
                {
                    _logger.LogWarning("ProfileOverview: Account not found for user ID {UserId}", userId);
                    return NotFound();
                }

                _logger.LogInformation("ProfileOverview: Loading for user {UserId}", userId);
                return PartialView("~/Views/Home/_ProfileOverview.cshtml", account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile overview");
                return StatusCode(500, "Đã xảy ra lỗi khi tải thông tin.");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string accountName, string? phoneNumber, IFormFile? avatar)
        {
            try
            {
                _logger.LogInformation("UpdateProfile called with accountName: {AccountName}, phoneNumber: {PhoneNumber}, hasAvatar: {HasAvatar}",
                    accountName, phoneNumber, avatar != null);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("UpdateProfile: No user ID found in claims");
                    return Json(new { success = false, message = "Không thể xác định người dùng." });
                }

                var account = await _accountService.GetByIdAsync(int.Parse(userId));
                if (account == null)
                {
                    _logger.LogWarning("UpdateProfile: Account not found for user ID {UserId}", userId);
                    return Json(new { success = false, message = "Không tìm thấy tài khoản." });
                }

                _logger.LogInformation("UpdateProfile: Current account - Name: {Name}, Email: {Email}, Phone: {Phone}",
                    account.AccountName, account.Email, account.PhoneNumber);

                // Validate account name
                if (string.IsNullOrWhiteSpace(accountName))
                    return Json(new { success = false, message = "Tên hiển thị không được để trống." });

                // Update basic info
                account.AccountName = accountName.Trim();
                account.PhoneNumber = phoneNumber?.Trim();

                // Update avatar if provided
                string? newAvatarPath = null;
                if (avatar != null && avatar.Length > 0)
                {
                    _logger.LogInformation("UpdateProfile: Processing avatar upload - FileName: {FileName}, Size: {Size}",
                        avatar.FileName, avatar.Length);

                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)." });

                    // Validate file size (max 5MB)
                    if (avatar.Length > 5 * 1024 * 1024)
                        return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 5MB." });

                    var fileName = Guid.NewGuid() + extension;
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");

                    // Create directory if not exists
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }

                    newAvatarPath = $"/uploads/avatars/{fileName}";
                    account.Image = newAvatarPath;
                    _logger.LogInformation("UpdateProfile: Avatar saved to {Path}", newAvatarPath);
                }

                _logger.LogInformation("UpdateProfile: About to call AccountService.UpdateAsync for user {UserId}", userId);

                // Update account (email will remain the same as per AccountService validation)
                await _accountService.UpdateAsync(account.Id, account);

                _logger.LogInformation("UpdateProfile: Successfully updated account for user {UserId}", userId);

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thông tin thành công!",
                    avatar = newAvatarPath ?? account.Image,
                    accountName = account.AccountName,
                    phoneNumber = account.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile: {Message}", ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Không thể xác định người dùng." });

                // Validation
                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });

                if (newPassword.Length < 8)
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 8 ký tự." });

                if (newPassword != confirmPassword)
                    return Json(new { success = false, message = "Mật khẩu mới và xác nhận mật khẩu không khớp." });

                var account = await _accountService.GetByIdAsync(int.Parse(userId));
                if (account == null)
                    return Json(new { success = false, message = "Không tìm thấy tài khoản." });

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, account.Password))
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng." });

                // Update password
                account.Password = newPassword; // AccountService will hash it
                await _accountService.UpdateAsync(account.Id, account);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return Json(new
                {
                    success = true,
                    message = "Đổi mật khẩu thành công!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        /// <summary>
        /// API endpoint to get user's saved addresses for checkout
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUserAddresses()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId == 0)
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });

                var addresses = await _addressService.ListAsync(userId);

                var result = addresses.Select(a => new
                {
                    id = a.Id,
                    addressLine = a.AddressLine,
                    city = a.City,
                    ward = a.Ward,
                    isDefault = a.IsDefault,
                    fullAddress = $"{a.AddressLine}, {a.Ward}, {a.City}".Replace(", , ", ", ")
                }).OrderByDescending(a => a.isDefault).ToList();

                return Json(new { success = true, addresses = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user addresses");
                return Json(new { success = false, message = "Không thể tải địa chỉ" });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}