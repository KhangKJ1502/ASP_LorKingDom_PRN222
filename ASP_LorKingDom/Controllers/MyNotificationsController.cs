using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    // [Authorize]
    public class MyNotificationsController : Controller
    {
        private readonly INotificationService _svc;

        public MyNotificationsController(INotificationService svc) => _svc = svc;

        /// <summary>
        /// Hiển thị danh sách thông báo của user hiện tại
        /// </summary>
        /// <param name="isRead">null = tất cả, true = đã đọc, false = chưa đọc</param>
        /// <param name="page">Trang hiện tại</param>
        /// <param name="pageSize">Số lượng item mỗi trang</param>
        public async Task<IActionResult> Index(bool? isRead = null, int page = 1, int pageSize = 20)
        {
            int currentUserId = GetCurrentUserId();
            var data = await _svc.GetMyNotificationsAsync(currentUserId, isRead, page, pageSize);

            ViewBag.CurrentFilter = isRead;
            return View("~/Views/MyNotifications/Index.cshtml", data);
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc (bảo vệ cơ bản: chỉ cho phép nếu id thuộc user hiện tại)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, bool? returnFilter = null)
        {
            try
            {
                int currentUserId = GetCurrentUserId();

                // Bảo vệ cơ bản: kiểm tra id có thuộc user không (fetch 1 trang lớn)
                var myPage = await _svc.GetMyNotificationsAsync(currentUserId, null, 1, 1000);
                var owned = myPage.Items.Any(n => n.UserNotificationId == id);
                if (!owned)
                {
                    TempData["Error"] = "Bạn không có quyền đánh dấu thông báo này.";
                    return RedirectToAction(nameof(Index), new { isRead = returnFilter });
                }

                await _svc.MarkReadAsync(id);
                TempData["Success"] = "Đã đánh dấu thông báo là đã đọc.";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { isRead = returnFilter });
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo chưa đọc là đã đọc
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                var unread = await _svc.GetMyNotificationsAsync(currentUserId, false, 1, 1000);

                foreach (var notif in unread.Items)
                {
                    if (!notif.IsRead)
                        await _svc.MarkReadAsync(notif.UserNotificationId);
                }

                TempData["Success"] = $"Đã đánh dấu tất cả {unread.Items.Count} thông báo là đã đọc.";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// API endpoint để lấy số lượng thông báo chưa đọc (badge/counter)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                int currentUserId = GetCurrentUserId();
                var data = await _svc.GetMyNotificationsAsync(currentUserId, false, 1, 1);
                return Json(new { success = true, count = data.Total });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }
    }
}
