using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IEmailOtpService _emailOtpService;
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;

        public AuthController(
            IEmailOtpService emailOtpService,
            IAccountService accountService,
            IRoleService roleService)
        {
            _emailOtpService = emailOtpService;
            _accountService = accountService;
            _roleService = roleService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            try
            {
                AuthValidator.ValidateLogin(email, password);
                // Kiểm tra thông tin đăng nhập qua BLL
                var user = await _accountService.AuthenticateAsync(email, password);
                if (user == null)
                {
                    TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                    return RedirectToAction("Login", "Auth", new { returnUrl });
                }

                var roleName = await _roleService.GetRoleNameByIdAsync(user.RoleId);

                // Tạo Claims cho người dùng
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, roleName ?? "Customer")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Đăng nhập và tạo cookie
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = rememberMe, // Cookie tồn tại lâu dài nếu ghi nhớ
                        ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                    });

                TempData["Success"] = "Đăng nhập thành công! Chào mừng bạn trở lại.";
                // Chuyển hướng về trang gốc hoặc returnUrl
                return Redirect(returnUrl ?? "/");
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Login", "Auth", new { returnUrl });
            }
            catch (Exception ex) // Lỗi bất ngờ khác
            {
                TempData["Error"] = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.";
                return RedirectToAction("Login", "Auth", new { returnUrl });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Signup");
            }

            // Kiểm tra email đã tồn tại
            if (await _accountService.ExistsByEmailAsync(email))
            {
                TempData["Error"] = "Email đã được sử dụng.";
                return RedirectToAction("Signup");
            }

            // Gửi OTP
            try
            {
                await _emailOtpService.SendOtpAsync(email);
                TempData["Email"] = email;
                TempData["Password"] = password;
                return RedirectToAction("VerifyOtp");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Signup");
            }
        }

        // ...existing code...

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui lòng nhập email.";
                return RedirectToAction("ForgotPassword");
            }

            // Kiểm tra email tồn tại
            if (!await _accountService.ExistsByEmailAsync(email))
            {
                TempData["Error"] = "Email không tồn tại trong hệ thống.";
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                // Tạo mật khẩu mới ngẫu nhiên (8 ký tự)
                var newPassword = GenerateRandomPassword(8);

                // Reset mật khẩu trong DB
                var success = await _accountService.ResetPasswordAsync(email, newPassword);
                if (!success)
                {
                    TempData["Error"] = "Không thể đặt lại mật khẩu. Vui lòng thử lại.";
                    return RedirectToAction("ForgotPassword");
                }

                // Gửi email
                await _emailOtpService.SendPasswordResetEmailAsync(email, newPassword);

                TempData["Success"] = "Mật khẩu mới đã được gửi qua email. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("ForgotPassword");
            }
        }

        // Helper method để tạo mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // ...existing code...

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["Email"] as string;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Signup");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(
            string email,
            string? otpCode,
            string Otp1, string Otp2, string Otp3, string Otp4, string Otp5, string Otp6)
        {
            // Ghép OTP nếu client chưa gửi otpCode
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                otpCode = string.Concat(Otp1, Otp2, Otp3, Otp4, Otp5, Otp6)?.Trim();
            }

            // ✅ 1. Lấy lại password tạm lưu trong TempData
            var password = TempData["Password"] as string;
            if (string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại.";
                return RedirectToAction("Signup");
            }

            // ✅ 2. Xác minh OTP
            if (await _emailOtpService.VerifyOtpAsync(email, otpCode))
            {
                // Tạo tài khoản (hash password, lưu vào DB)
                var account = new AccountDto
                {
                    RoleId = 4, // Khách hàng
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    AccountName = email.Split('@')[0],
                    Status = "Active",
                    CreatedAt = DateTime.Now,
                    Provider = "LOCAL"
                };
                try
                {
                    await _accountService.CreateAsync(account);
                    await _emailOtpService.SendWelcomeEmailAsync(email, account.AccountName);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo tài khoản: " + ex.Message);
                    ViewBag.Email = email;
                    TempData.Keep("Password");
                    return View();
                }

                TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Auth");
            }
            else
            {
                ModelState.AddModelError("", "OTP không hợp lệ hoặc đã hết hạn.");
                ViewBag.Email = email;
                TempData.Keep("Password");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            try
            {
                await _emailOtpService.SendOtpAsync(email);
                TempData["Message"] = "OTP đã được gửi lại.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            TempData["Email"] = email;
            return RedirectToAction("VerifyOtp");
        }
    }
}
