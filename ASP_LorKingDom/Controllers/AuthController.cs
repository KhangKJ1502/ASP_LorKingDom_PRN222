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
                var user = await _accountService.AuthenticateAsync(email, password);
                if (user == null)
                    return BadRequest(new { success = false, message = "Email hoặc mật khẩu không đúng." });

                var roleName = await _roleService.GetRoleNameByIdAsync(user.RoleId);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, roleName ?? "Customer")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = rememberMe,
                        ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                    });

                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công! Chào mừng bạn trở lại.",
                    redirectUrl = returnUrl ?? "/"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi đăng nhập." });
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
                return BadRequest(new { success = false, message = "Mật khẩu xác nhận không khớp." });

            if (await _accountService.ExistsByEmailAsync(email))
                return BadRequest(new { success = false, message = "Email đã được sử dụng." });

            try
            {
                await _emailOtpService.SendOtpAsync(email);
                // Store in session instead of TempData for AJAX
                HttpContext.Session.SetString("SignupEmail", email);
                HttpContext.Session.SetString("SignupPassword", password);

                return Ok(new
                {
                    success = true,
                    message = "OTP đã được gửi. Vui lòng kiểm tra email của bạn.",
                    redirectUrl = "/Auth/VerifyOtp"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi gửi OTP." });
            }
        }


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
                return BadRequest(new { success = false, message = "Vui lòng nhập email." });

            if (!await _accountService.ExistsByEmailAsync(email))
                return BadRequest(new { success = false, message = "Email không tồn tại trong hệ thống." });

            try
            {
                var newPassword = GenerateRandomPassword(8);
                var success = await _accountService.ResetPasswordAsync(email, newPassword);

                if (!success)
                    return BadRequest(new { success = false, message = "Không thể đặt lại mật khẩu. Vui lòng thử lại." });

                await _emailOtpService.SendPasswordResetEmailAsync(email, newPassword);

                return Ok(new
                {
                    success = true,
                    message = "Mật khẩu mới đã được gửi qua email. Vui lòng kiểm tra hộp thư."
                });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi đặt lại mật khẩu." });
            }
        }

        // Helper method để tạo mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = HttpContext.Session.GetString("SignupEmail");
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
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                otpCode = string.Concat(Otp1 ?? "", Otp2 ?? "", Otp3 ?? "", Otp4 ?? "", Otp5 ?? "", Otp6 ?? "")?.Trim();
            }

            var password = HttpContext.Session.GetString("SignupPassword");
            if (string.IsNullOrEmpty(password))
                return BadRequest(new { success = false, message = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại." });

            if (string.IsNullOrEmpty(otpCode))
                return BadRequest(new { success = false, message = "Vui lòng nhập OTP." });

            try
            {
                if (await _emailOtpService.VerifyOtpAsync(email, otpCode))
                {
                    var account = new AccountDto
                    {
                        RoleId = 4,
                        Email = email,
                        Password = password,
                        AccountName = email.Split('@')[0],
                        Status = "Active",
                        CreatedAt = DateTime.Now,
                        Provider = "LOCAL"
                    };

                    try
                    {
                        await _accountService.CreateAsync(account);
                        await _emailOtpService.SendWelcomeEmailAsync(email, account.AccountName);

                        // Clear session
                        HttpContext.Session.Remove("SignupEmail");
                        HttpContext.Session.Remove("SignupPassword");

                        return Ok(new
                        {
                            success = true,
                            message = "Đăng ký thành công! Vui lòng đăng nhập để tiếp tục.",
                            redirectUrl = "/Auth/Login"
                        });
                    }
                    catch
                    {
                        return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi tạo tài khoản." });
                    }
                }
                else
                {
                    return BadRequest(new { success = false, message = "OTP không hợp lệ hoặc đã hết hạn." });
                }
            }
            catch
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi xác minh OTP." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            try
            {
                await _emailOtpService.SendOtpAsync(email);
                return Ok(new
                {
                    success = true,
                    message = "OTP đã được gửi lại. Vui lòng kiểm tra email của bạn."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi khi gửi lại OTP." });
            }
        }
    }
}
