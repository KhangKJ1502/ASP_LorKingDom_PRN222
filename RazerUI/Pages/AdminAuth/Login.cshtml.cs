using BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RazerUI.Pages.AdminAuth
{
    public class LoginModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            IAccountService accountService,
            IRoleService roleService,
            ILogger<LoginModel> logger)
        {
            _accountService = accountService;
            _roleService = roleService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? RedirectUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }

            public string? ReturnUrl { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            Input.ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
                return Page();
            }

            try
            {
                var email = Input.Email.Trim().ToLower();
                var password = Input.Password;

                // Authenticate user
                var user = await _accountService.AuthenticateAsync(email, password);
                if (user == null)
                {
                    ErrorMessage = "Email hoặc mật khẩu không đúng.";
                    return Page();
                }

                // Get role name
                var roleName = await _roleService.GetRoleNameByIdAsync(user.RoleId);

                // Check if user is Admin/Staff/Warehouse
                if (roleName != "Admin" && roleName != "Staff" && roleName != "WareHouse")
                {
                    ErrorMessage = "Bạn không có quyền truy cập vào hệ thống quản lý.";
                    return Page();
                }

                // Create claims
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

                // Sign in
                await HttpContext.SignInAsync("AdminScheme", principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = Input.RememberMe,
                        ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                    });

                _logger.LogInformation("User {Email} logged in as {Role}", email, roleName);

                // Base URL của WebUI
                const string webUIBaseUrl = "https://localhost:7777";

                // Nếu có returnUrl từ WebUI
                if (!string.IsNullOrEmpty(Input.ReturnUrl))
                {
                    // Nếu returnUrl đã là absolute URL (có http/https), dùng trực tiếp
                    if (Input.ReturnUrl.StartsWith("http://") || Input.ReturnUrl.StartsWith("https://"))
                    {
                        return Redirect(Input.ReturnUrl);
                    }

                    // Nếu là relative path (/Blog/Manage), thêm WebUI base URL
                    var absoluteUrl = Input.ReturnUrl.StartsWith("/")
                        ? $"{webUIBaseUrl}{Input.ReturnUrl}"
                        : $"{webUIBaseUrl}/{Input.ReturnUrl}";

                    return Redirect(absoluteUrl);
                }

                // Nếu không có returnUrl, redirect về WebUI dashboard tương ứng role
                string webUIRedirect;
                if (roleName == "WareHouse" || roleName == "Admin") // Lưu ý: WareHouse có H viết hoa
                {
                    webUIRedirect = $"{webUIBaseUrl}/Statistics/ProductStatistics";
                }
                else if (roleName == "Staff" || roleName == "Admin")
                {
                    webUIRedirect = $"{webUIBaseUrl}/Statistics/Dashboard";
                }
                else
                {
                    webUIRedirect = $"{webUIBaseUrl}/Statistics/Dashboard";
                }

                return Redirect(webUIRedirect);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return Page();
            }
        }
    }
}
