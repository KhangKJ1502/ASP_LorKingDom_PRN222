using BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class AdminAuthController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;

        public AdminAuthController(IAccountService accountService, IRoleService roleService)
        {
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
                email = email?.Trim().ToLower() ?? "";

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });

                var user = await _accountService.AuthenticateAsync(email, password);
                if (user == null)
                    return BadRequest(new { success = false, message = "Email hoặc mật khẩu không đúng." });

                var roleName = await _roleService.GetRoleNameByIdAsync(user.RoleId);

                // Kiểm tra role có phải Admin/Staff/Warehouse không
                if (roleName != "Admin" && roleName != "Staff" && roleName != "Warehouse")
                {
                    return BadRequest(new { success = false, message = "Bạn không có quyền truy cập vào hệ thống quản lý." });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, roleName ?? "Staff"),
                    new Claim("AccountName", user.AccountName ?? email.Split('@')[0]),
                    new Claim("Avatar", user.Image ?? "/Assets/image/avatar/avatar-illustrated-02.png")
                };

                var identity = new ClaimsIdentity(claims, "AdminScheme");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("AdminScheme", principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = rememberMe,
                        ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                    });

                return Ok(new
                {
                    success = true,
                    message = $"Đăng nhập thành công! Chào mừng {roleName}.",
                    redirectUrl = returnUrl ?? "/Admin/Dashboard"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminScheme");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
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
    }
}
