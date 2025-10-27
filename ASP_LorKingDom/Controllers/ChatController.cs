using DAL.Interfaces;           // IChatStoreRepository
using DAL.Models;               // OnlineStaff
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly IChatStoreRepository _store;

        public ChatController(
            ILogger<ChatController> logger,
            IChatStoreRepository store)
        {
            _logger = logger;
            _store = store;
        }

        // =========================================
        // CUSTOMER WIDGET (guest allowed)
        // =========================================
        [AllowAnonymous]
        public IActionResult Customer()
        {
            _logger.LogInformation("=== CUSTOMER CHAT PAGE ===");

            // --- 1. Xác định customerId + displayName ---
            var cookieUserId = Request.Cookies["customerId"];
            var cookieName = Request.Cookies["customerName"];

            string? claimUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? claimUserName =
                User.FindFirst("DisplayName")?.Value
                ?? User.Identity?.Name;

            string finalUserId;
            string finalName;

            if (!string.IsNullOrWhiteSpace(cookieUserId))
            {
                // KH từng chat -> ưu tiên cookie
                finalUserId = cookieUserId!;
                finalName = !string.IsNullOrWhiteSpace(cookieName)
                                ? cookieName!
                                : (claimUserName ?? "customer");
            }
            else if (!string.IsNullOrWhiteSpace(claimUserId))
            {
                // KH đã login (customer có account)
                finalUserId = claimUserId!;
                finalName = claimUserName ?? "customer";
            }
            else
            {
                // Guest mới
                finalUserId = $"guest_{Guid.NewGuid():N}";
                finalName = "Guest";
                _logger.LogInformation($"[Customer()] Tạo guest session {finalUserId}");
            }

            // Ghi cookie cho FE (SignalR dùng)
            Response.Cookies.Append("customerId", finalUserId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false,
                Secure = false, // set true nếu HTTPS prod
                IsEssential = true
            });

            Response.Cookies.Append("customerName", finalName, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false,
                Secure = false,
                IsEssential = true
            });

            // --- 2. giữ lại staff mà KH từng chat nếu có ---
            var existedStaffId = Request.Cookies["staffId"];
            var existedStaffName = Request.Cookies["staffName"];

            if (!string.IsNullOrWhiteSpace(existedStaffId))
            {
                Response.Cookies.Append("staffId", existedStaffId!, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    HttpOnly = false,
                    IsEssential = true
                });
            }

            if (!string.IsNullOrWhiteSpace(existedStaffName))
            {
                Response.Cookies.Append("staffName", existedStaffName!, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    HttpOnly = false,
                    IsEssential = true
                });
            }

            // --- 3. Lấy danh sách STAFF ONLINE từ _store ---
            // _store.Users được cập nhật trong ChatService.UserConnectedAsync()
            // khi staff connect hub với isStaff=true
            var onlineStaffList = _store.Users
                .Values
                .Where(u => u.IsStaff)
                .Where(u => _store.IsOnline(u.UserId))
                .Select(u => new OnlineStaff
                {
                    StaffId = u.UserId, // ví dụ "22" (trùng ClaimTypes.NameIdentifier khi staff login)
                    StaffName = string.IsNullOrWhiteSpace(u.DisplayName)
                                ? "Nhân viên hỗ trợ"
                                : u.DisplayName
                })
                .ToList();

            // --- 4. đẩy info xuống View ---
            ViewBag.UserId = finalUserId;
            ViewBag.Name = finalName;

            ViewBag.SelectedStaffIdFromCookie = existedStaffId; // staff KH đã gán trước đó
            ViewBag.OnlineStaff = onlineStaffList;              // danh sách online ngay lúc render

            return View();
        }

        // =========================================
        // STAFF DASHBOARD (AdminScheme)
        // =========================================
        [Authorize(
      AuthenticationSchemes = "AdminScheme",
      Roles = "Admin,Staff,Warehouse"
  )]
        public IActionResult Staff()
        {
            _logger.LogInformation("=== STAFF DASHBOARD PAGE ===");

            // 1. Lấy từ Claims (chuẩn nhất, từ DB sau login)
            string? claimId = User.FindFirstValue(ClaimTypes.NameIdentifier); // ví dụ "22"
            string? claimRole = User.FindFirstValue(ClaimTypes.Role);         // "Staff", "Admin", ...
            string? claimName = User.FindFirst("AccountName")?.Value
                                ?? User.Identity?.Name
                                ?? "Staff";

            // 2. Fallback lấy từ cookie cũ nếu vì lý do gì đó mất claims
            string? staffIdFromCookie = Request.Cookies["staffId"];
            string? staffNameFromCookie = Request.Cookies["staffName"];

            // 3. Chọn finalStaffId / finalStaffName
            string finalStaffId = !string.IsNullOrWhiteSpace(claimId)
                ? claimId!
                : (staffIdFromCookie ?? string.Empty);

            string finalStaffName = !string.IsNullOrWhiteSpace(claimName)
                ? claimName
                : (staffNameFromCookie ?? "Staff");

            // 4. Nếu không có ID -> coi như chưa login hợp lệ
            if (string.IsNullOrWhiteSpace(finalStaffId))
            {
                _logger.LogWarning("[Staff()] Không tìm thấy staffId -> 401");
                return Unauthorized("Bạn chưa đăng nhập.");
            }

            // 5. Check role
            if (string.IsNullOrWhiteSpace(claimRole)
                || !(claimRole == "Admin" || claimRole == "Staff" || claimRole == "Warehouse"))
            {
                _logger.LogWarning($"[Staff()] Role không hợp lệ: {claimRole} -> 403");
                return Forbid("Bạn không có quyền truy cập trang hỗ trợ khách hàng.");
            }

            // 6. Ghi cookie để FE (SignalR) đọc
            Response.Cookies.Append("staffId", finalStaffId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false,
                IsEssential = true
            });

            Response.Cookies.Append("staffName", finalStaffName, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false,
                IsEssential = true
            });

            _logger.LogInformation($"[Staff()] OK -> {finalStaffId} / {finalStaffName} / role={claimRole}");

            // 7. Gửi xuống View để hiện tên, v.v.
            ViewBag.UserId = finalStaffId;
            ViewBag.Name = finalStaffName;

            return View();
        }

    }
}
