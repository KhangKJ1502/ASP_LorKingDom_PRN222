using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    public class MyNotificationsController : Controller
    {
        private readonly INotificationService _svc;

        public MyNotificationsController(INotificationService svc) => _svc = svc;

        /// <summary>
        /// Hiển thị danh sách thông báo của user hiện tại
        /// </summary>
        public async Task<IActionResult> Index(bool? isRead = null, int page = 1, int pageSize = 20)
        {
            int currentUserId = GetCurrentUserId();
            var data = await _svc.GetMyNotificationsAsync(currentUserId, isRead, page, pageSize);

            ViewBag.CurrentFilter = isRead;
            return View("~/Views/MyNotifications/Index.cshtml", data);
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, bool? returnFilter = null)
        {
            try
            {
                int currentUserId = GetCurrentUserId();

                // Kiểm tra quyền: chỉ user sở hữu mới được đánh dấu
                var myNotifications = await _svc.GetMyNotificationsAsync(currentUserId, null, 1, 1000);
                var owned = myNotifications.Items.Any(n => n.UserNotificationId == id);

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

                foreach (var notif in unread.Items.Where(n => !n.IsRead))
                {
                    await _svc.MarkReadAsync(notif.UserNotificationId);
                }

                TempData["Success"] = $"Đã đánh dấu {unread.Items.Count} thông báo là đã đọc.";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// API endpoint để lấy số lượng thông báo chưa đọc (cho badge)
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