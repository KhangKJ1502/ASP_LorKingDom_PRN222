using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class MyNotificationsController : Controller
    {
        private readonly IUserNotificationService _svc;

        public MyNotificationsController(IUserNotificationService svc)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool? isRead = null, int page = 1, int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem thông báo.";
                    return RedirectToAction("Login", "Account");
                }

                var data = await _svc.GetMyNotificationsAsync(currentUserId, isRead, page, pageSize);
                ViewBag.CurrentFilter = isRead;
                return View("~/Views/MyNotifications/Index.cshtml", data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải thông báo: {ex.Message}";
                return View("~/Views/MyNotifications/Index.cshtml",
                    new PagedResult<UserNotificationDto>
                    {
                        Items = Array.Empty<UserNotificationDto>().ToList(),
                        Total = 0,
                        Page = page,
                        PageSize = pageSize
                    });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, bool? returnFilter = null)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID thông báo không hợp lệ.";
                    return RedirectToAction(nameof(Index), new { isRead = returnFilter });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    TempData["Error"] = "Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Account");
                }

                var notification = await _svc.GetUserNotificationByIdAsync(id);
                if (notification == null)
                {
                    TempData["Error"] = "Không tìm thấy thông báo.";
                    return RedirectToAction(nameof(Index), new { isRead = returnFilter });
                }

                if (notification.UserId != currentUserId)
                {
                    TempData["Error"] = "Bạn không có quyền đánh dấu thông báo này.";
                    return RedirectToAction(nameof(Index), new { isRead = returnFilter });
                }

                if (notification.IsRead)
                {
                    TempData["Info"] = "Thông báo đã được đánh dấu đọc trước đó.";
                    return RedirectToAction(nameof(Index), new { isRead = returnFilter });
                }

                await _svc.MarkReadAsync(id);
                TempData["Success"] = "Đã đánh dấu thông báo là đã đọc.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện hành động này.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { isRead = returnFilter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    TempData["Error"] = "Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Account");
                }

                var markedCount = await _svc.MarkAllReadAsync(currentUserId);

                TempData["Success"] = markedCount > 0
                    ? $"Đã đánh dấu {markedCount} thông báo là đã đọc."
                    : "Không có thông báo chưa đọc.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                    return Json(new { success = false, message = "Chưa đăng nhập" });

                var count = await _svc.GetUnreadCountAsync(currentUserId);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckNewNotifications(DateTime? lastCheck = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                    return Json(new { success = false, hasNew = false });

                var unread = await _svc.GetMyNotificationsAsync(currentUserId, false, 1, 10);

                bool hasNew;
                DateTime? latest = unread.Items.FirstOrDefault()?.DeliveredAt ?? unread.Items.FirstOrDefault()?.ScheduledAt;

                if (lastCheck.HasValue)
                {
                    hasNew = unread.Items.Any(n =>
                        (n.DeliveredAt ?? n.ScheduledAt) > lastCheck.Value);
                }
                else
                {
                    hasNew = unread.Total > 0;
                }

                return Json(new { success = true, hasNew, count = unread.Total, latest });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            try
            {
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(claim)) return 0;
                return int.TryParse(claim, out var id) && id > 0 ? id : 0;
            }
            catch { return 0; }
        }
    }
}
