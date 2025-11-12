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
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool? isRead = null, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                if (IsJsonRequest())
                    return Json(new { success = false, message = "Chưa đăng nhập" });

                TempData["Error"] = "Vui lòng đăng nhập để xem thông báo.";
                return RedirectToAction("Login", "Auth");
            }

            // DỮ LIỆU CHO TAB HIỆN TẠI (có filter isRead, có phân trang)
            var PagedResult = await _svc.GetMyNotificationsAsync(currentUserId, isRead, page, pageSize);
            var totalUnread = await _svc.GetUnreadCountAsync(currentUserId);
            var allPage = await _svc.GetMyNotificationsAsync(currentUserId, null, 1, 1);
            var totalAll = allPage.Total;
            var totalRead = totalAll - totalUnread;
            if (totalRead < 0) totalRead = 0;

            // đẩy các số này ra ViewBag để View xài
            ViewBag.TotalAll = totalAll;
            ViewBag.TotalUnread = totalUnread;
            ViewBag.TotalRead = totalRead;
            ViewBag.CurrentFilter = isRead;

            // Nếu request là JSON (AJAX)
            if (IsJsonRequest())
            {
                return Json(new
                {
                    success = true,
                    items = PagedResult.Items,
                    total = PagedResult.Total,
                    page = PagedResult.Page,
                    pageSize = PagedResult.PageSize,
                    totalPages = PagedResult.TotalPages,
                    totalAll,
                    totalUnread,
                    totalRead
                });
            }

            return View("~/Views/MyNotifications/Index.cshtml", PagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestUnread()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var unread = await _svc.GetMyNotificationsAsync(currentUserId, false, 1, 10);

            var shaped = unread.Items.Select(n => new
            {
                id = n.UserNotificationId,
                title = n.Title,
                message = n.Message,
                at = (n.DeliveredAt ?? n.ScheduledAt).ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                timeAgo = GetTimeAgo(n.DeliveredAt ?? n.ScheduledAt),
            });

            return Json(new
            {
                success = true,
                items = shaped,
                totalUnread = unread.Total
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, bool? returnFilter = null)
        {
            if (id <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "ID không hợp lệ" });

                TempData["Error"] = "ID thông báo không hợp lệ.";
                return RedirectToAction(nameof(Index), new { isRead = returnFilter });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Chưa đăng nhập" });

                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var notification = await _svc.GetUserNotificationByIdAsync(id);
            if (notification == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Không tìm thấy thông báo" });

                TempData["Error"] = "Không tìm thấy thông báo.";
                return RedirectToAction(nameof(Index), new { isRead = returnFilter });
            }

            if (notification.UserId != currentUserId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Không có quyền" });

                TempData["Error"] = "Bạn không có quyền đánh dấu thông báo này.";
                return RedirectToAction(nameof(Index), new { isRead = returnFilter });
            }

            if (notification.IsRead)
            {
                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Đã đọc trước đó", alreadyRead = true });

                TempData["Info"] = "Thông báo đã được đánh dấu đọc trước đó.";
                return RedirectToAction(nameof(Index), new { isRead = returnFilter });
            }

            await _svc.MarkReadAsync(id);

            var unreadCount = await _svc.GetUnreadCountAsync(currentUserId);

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Đã đánh dấu đọc", unreadCount });

            TempData["Success"] = "Đã đánh dấu thông báo là đã đọc.";
            return RedirectToAction(nameof(Index), new { isRead = returnFilter });
        }

        // phiên bản AJAX riêng để offcanvas gọi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReadAjax(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var notification = await _svc.GetUserNotificationByIdAsync(id);
            if (notification == null || notification.UserId != currentUserId)
                return Json(new { success = false, message = "Không tìm thấy / Không có quyền" });

            if (!notification.IsRead)
            {
                await _svc.MarkReadAsync(id);
            }

            // cập nhật lại số chưa đọc cho badge
            var unreadCount = await _svc.GetUnreadCountAsync(currentUserId);

            return Json(new { success = true, unread = unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Chưa đăng nhập" });

                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var markedCount = await _svc.MarkAllReadAsync(currentUserId);

            if (IsAjaxRequest())
            {
                var unreadCount = await _svc.GetUnreadCountAsync(currentUserId);
                return Json(new
                {
                    success = true,
                    count = markedCount,
                    unread = unreadCount
                });
            }

            TempData["Success"] = markedCount > 0
                ? $"Đã đánh dấu {markedCount} thông báo là đã đọc."
                : "Không có thông báo chưa đọc.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
                return Json(new { success = false, count = 0, message = "Chưa đăng nhập" });

            var count = await _svc.GetUnreadCountAsync(currentUserId);
            return Json(new { success = true, count });
        }

        [HttpGet]
        public async Task<IActionResult> CheckNewNotifications(DateTime? lastCheck = null)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
                return Json(new { success = false, hasNew = false });

            var unread = await _svc.GetMyNotificationsAsync(currentUserId, false, 1, 10);

            var latestItem = unread.Items.FirstOrDefault();
            DateTime? latestTime = latestItem != null
                ? (latestItem.DeliveredAt ?? latestItem.ScheduledAt)
                : null;

            bool hasNew;
            if (lastCheck.HasValue && latestTime.HasValue)
            {
                hasNew = latestTime.Value > lastCheck.Value;
            }
            else
            {
                hasNew = unread.Total > 0;
            }

            return Json(new
            {
                success = true,
                hasNew,
                count = unread.Total,
                latest = latestTime
            });
        }

        // ==================== HELPER ====================

        private int GetCurrentUserId()
        {
            try
            {
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(claim)) return 0;
                return int.TryParse(claim, out var id) && id > 0 ? id : 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool IsJsonRequest()
        {
            return Request.Headers["Accept"].ToString().Contains("application/json");
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers["Accept"].ToString().Contains("application/json");
        }

        private static string GetTimeAgo(DateTime? dt)
        {
            if (!dt.HasValue) return "";
            var local = dt.Value.Kind == DateTimeKind.Utc ? dt.Value.ToLocalTime() : dt.Value;
            var span = DateTime.Now - local;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return local.ToString("dd/MM/yyyy HH:mm");
        }
    }
}
