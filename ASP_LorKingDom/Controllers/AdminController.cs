using BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.Filters;

namespace ASP_LorKingDom.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    public class AdminController : Controller
    {
        private readonly IAccountService _accountService;

        public AdminController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        [ManagementOnly] // All roles: Profile
        public async Task<IActionResult> Profile()
        {
            string? role = "";
            var adminAuth = await HttpContext.AuthenticateAsync("AdminScheme");
            if (adminAuth.Succeeded)
            {
                role = adminAuth.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            }
            ViewBag.Role = role;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ManagementOnly] // All roles: Update Profile
        public async Task<JsonResult> UpdateProfile(string accountName, string phoneNumber, bool changePassword = false,
            string? currentPassword = null, string? newPassword = null, IFormFile? avatar = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Không thể xác định người dùng" });

                var user = await _accountService.GetByIdAsync(int.Parse(userId));
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });

                // Update basic info
                user.AccountName = accountName?.Trim() ?? user.AccountName;
                user.PhoneNumber = phoneNumber?.Trim();

                // Update avatar if provided
                if (avatar != null && avatar.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(avatar.FileName);
                    var path = Path.Combine("wwwroot/uploads/avatars", fileName);
                    var directory = Path.GetDirectoryName(path);

                    // Create directory if not exists
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = System.IO.File.Create(path))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                    user.Image = $"/uploads/avatars/{fileName}";
                }

                // Update password if requested
                if (changePassword && !string.IsNullOrWhiteSpace(newPassword))
                {
                    if (string.IsNullOrWhiteSpace(currentPassword))
                        return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại" });

                    // Verify current password
                    if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
                        return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });

                    user.Password = newPassword;
                }

                await _accountService.UpdateAsync(user.Id, user);

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thông tin thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}