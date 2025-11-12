using BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class AdminAuthController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;
        private readonly IEmailOtpService _emailOtpService;

        public AdminAuthController(IAccountService accountService, IRoleService roleService, IEmailOtpService emailOtpService)
        {
            _accountService = accountService;
            _roleService = roleService;
            _emailOtpService = emailOtpService;
        }

        /// Redirect admin Login sang RazorUI project
        [HttpGet]
        public IActionResult RedirectToRazorUI(string? returnUrl = null)
        {
            // Encode returnUrl để pass qua RazorUI
            var encodedReturnUrl = string.IsNullOrEmpty(returnUrl)
                ? ""
                : $"?returnUrl={Uri.EscapeDataString(returnUrl)}";

            var razorUILoginUrl = $"https://localhost:7226/AdminAuth/Login{encodedReturnUrl}";
            return Redirect(razorUILoginUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminScheme");
            return RedirectToAction("RedirectToRazorUI"); // Login UI
        }

        [HttpGet]
        public async Task<IActionResult> AccessDeniedAsync()
        {
            string? role = "";

            var adminAuth = await HttpContext.AuthenticateAsync("AdminScheme");
            if (adminAuth.Succeeded)
                role = adminAuth.Principal?.FindFirst(ClaimTypes.Role)?.Value;

            ViewBag.Role = role;
            return View();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _accountService.GetByIdAsync(int.Parse(userId));
            if (user == null)
                return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff";

            return Json(new
            {
                accountName = user.AccountName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                image = user.Image,
                status = user.Status,
                role = role,
                createdAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });
        }

        // ===== FORGOT PASSWORD ACTIONS =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ForgotPasswordRequest(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Vui lòng nhập email." });

            email = email?.Trim().ToLower() ?? "";

            if (!await _accountService.ExistsByEmailAsync(email))
                return Json(new { success = false, message = "Email không tồn tại trong hệ thống." });

            try
            {
                // Gửi OTP cho quên mật khẩu
                await _emailOtpService.SendOtpAsync(email, "ForgotPasswordAdmin");

                // Lưu email vào session
                HttpContext.Session.SetString("ForgotPasswordAdminEmail", email);

                return Json(new
                {
                    success = true,
                    message = "OTP đã được gửi. Vui lòng kiểm tra email của bạn.",
                    redirectUrl = "/AdminAuth/VerifyForgotPasswordOtp"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi gửi OTP." });
            }
        }

        [HttpGet]
        public IActionResult VerifyForgotPasswordOtp()
        {
            var email = HttpContext.Session.GetString("ForgotPasswordAdminEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> VerifyForgotPasswordOtpConfirm(string email, string? otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
                return Json(new { success = false, message = "Vui lòng nhập OTP." });

            try
            {
                if (await _emailOtpService.VerifyOtpAsync(email, otpCode, "ForgotPasswordAdmin"))
                {
                    // Lưu email vào session để dùng ở trang Reset Password
                    HttpContext.Session.SetString("VerifiedForgotPasswordAdminEmail", email);

                    return Json(new
                    {
                        success = true,
                        message = "OTP hợp lệ.",
                        redirectUrl = "/AdminAuth/ResetPasswordForm"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "OTP không hợp lệ hoặc đã hết hạn." });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xác minh OTP." });
            }
        }

        [HttpGet]
        public IActionResult ResetPasswordForm()
        {
            var email = HttpContext.Session.GetString("VerifiedForgotPasswordAdminEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ mật khẩu." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });

            if (newPassword.Length < 8)
                return Json(new { success = false, message = "Mật khẩu phải tối thiểu 8 ký tự." });

            try
            {
                var success = await _accountService.ResetPasswordAsync(email, newPassword);

                if (!success)
                    return Json(new { success = false, message = "Không thể đặt lại mật khẩu." });

                // Xóa session
                HttpContext.Session.Remove("ForgotPasswordAdminEmail");
                HttpContext.Session.Remove("VerifiedForgotPasswordAdminEmail");

                return Json(new
                {
                    success = true,
                    message = "Mật khẩu đã được đặt lại thành công!",
                    redirectUrl = "/AdminAuth/Login"
                });
            }
            catch
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi đặt lại mật khẩu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResendForgotPasswordOtp(string email)
        {
            try
            {
                await _emailOtpService.SendOtpAsync(email, "ForgotPasswordAdmin");
                return Json(new
                {
                    success = true,
                    message = "OTP đã được gửi lại. Vui lòng kiểm tra email của bạn."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi gửi lại OTP." });
            }
        }
    }
}
