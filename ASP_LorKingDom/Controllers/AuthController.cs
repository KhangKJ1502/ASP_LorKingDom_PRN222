using BLL.DTOs;
using BLL.Interfaces;
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
        public async Task<JsonResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            try
            {
                //AuthValidator.ValidateLogin(email, password);
                var user = await _accountService.AuthenticateAsync(email, password);
                if (user == null)
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng." });

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

                return Json(new
                {
                    success = true,
                    message = "Đăng nhập thành công! Chào mừng bạn trở lại.",
                    redirectUrl = returnUrl ?? "/"
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi đăng nhập." });
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
        public async Task<JsonResult> Signup(SignupRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                // Tự động lấy tất cả lỗi từ Data Annotations
                var errorMessages = ModelState.Values
                                .SelectMany(v => v.Errors)
                                .Select(e => e.ErrorMessage);
                return Json(new { success = false, message = errorMessages });
            }

            if (await _accountService.ExistsByEmailAsync(dto.Email))
                return Json(new { success = false, message = "Email đã được sử dụng." });

            try
            {
                await _emailOtpService.SendOtpAsync(dto.Email);
                // Store in session instead of TempData for AJAX
                HttpContext.Session.SetString("SignupEmail", dto.Email);
                HttpContext.Session.SetString("SignupPassword", dto.Password);

                return Json(new
                {
                    success = true,
                    message = "OTP đã được gửi. Vui lòng kiểm tra email của bạn.",
                    redirectUrl = "/Auth/VerifyOtp"
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
        public async Task<JsonResult> VerifyOtp(
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
                return Json(new { success = false, message = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại." });

            if (string.IsNullOrEmpty(otpCode))
                return Json(new { success = false, message = "Vui lòng nhập OTP." });

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
                        var accountId = await _accountService.CreateAsync(account);
                        await _emailOtpService.SendWelcomeEmailAsync(email, account.AccountName);

                        // Clear session
                        HttpContext.Session.Remove("SignupEmail");
                        HttpContext.Session.Remove("SignupPassword");

                        // Tự động đăng nhập người dùng mới
                        var user = await _accountService.AuthenticateAsync(email, password);
                        if (user != null)
                        {
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
                                    IsPersistent = false,
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                                });
                        }

                        return Json(new
                        {
                            success = true,
                            message = "Đăng ký thành công! Tài khoản của bạn đã được tạo.",
                            redirectUrl = "/"
                        });
                    }
                    catch
                    {
                        return Json(new { success = false, message = "Đã xảy ra lỗi khi tạo tài khoản." });
                    }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResendOtp(string email)
        {
            try
            {
                await _emailOtpService.SendOtpAsync(email);
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ForgotPasswordRequest(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Vui lòng nhập email." });

            if (!await _accountService.ExistsByEmailAsync(email))
                return Json(new { success = false, message = "Email không tồn tại trong hệ thống." });

            try
            {
                // Gửi OTP cho quên mật khẩu
                await _emailOtpService.SendOtpAsync(email, "ForgotPassword");

                // Lưu email vào session
                HttpContext.Session.SetString("ForgotPasswordEmail", email);

                return Json(new
                {
                    success = true,
                    message = "OTP đã được gửi. Vui lòng kiểm tra email của bạn.",
                    redirectUrl = "/Auth/VerifyForgotPasswordOtp"
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
            var email = HttpContext.Session.GetString("ForgotPasswordEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> VerifyForgotPasswordOtp(string email, string? otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
                return Json(new { success = false, message = "Vui lòng nhập OTP." });

            try
            {
                if (await _emailOtpService.VerifyOtpAsync(email, otpCode, "ForgotPassword"))
                {
                    // Lưu email vào session để dùng ở trang Reset Password
                    HttpContext.Session.SetString("VerifiedForgotPasswordEmail", email);

                    return Json(new
                    {
                        success = true,
                        message = "OTP hợp lệ.",
                        redirectUrl = "/Auth/ResetPasswordForm"
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
            var email = HttpContext.Session.GetString("VerifiedForgotPasswordEmail");
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
                HttpContext.Session.Remove("ForgotPasswordEmail");
                HttpContext.Session.Remove("VerifiedForgotPasswordEmail");

                return Json(new
                {
                    success = true,
                    message = "Mật khẩu đã được đặt lại thành công!",
                    redirectUrl = "/Auth/Login"
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
                await _emailOtpService.SendOtpAsync(email, "ForgotPassword");
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
