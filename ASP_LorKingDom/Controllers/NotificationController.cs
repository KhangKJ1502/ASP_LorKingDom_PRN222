using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    // [Authorize(Roles = "Admin")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _svc;
        public NotificationController(INotificationService svc) => _svc = svc;

        public IActionResult Index() => RedirectToAction(nameof(Manage));

        // GET: /Notification/Manage
        public async Task<IActionResult> Manage([FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);
            var list = await _svc.SearchAsync(f);
            ViewBag.Filter = f;

            if (TempData["EditNotificationId"] is int nid && nid > 0)
            {
                var edit = await _svc.GetByIdAsync(nid);
                ViewBag.EditNotification = edit;
            }

            return View("~/Views/Admin/ManageNotification.cshtml", list);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, [FromQuery] NotificationFilterDto f)
        {
            var n = await _svc.GetByIdAsync(id);
            if (n == null)
            {
                TempData["Error"] = "Không tìm thấy thông báo cần sửa.";
                return RedirectToAction(nameof(Manage), NormalizeFilter(f));
            }

            TempData["EditNotificationId"] = id;
            return RedirectToAction(nameof(Manage), NormalizeFilter(f));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotification(NotificationSaveDto dto, [FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);
            try
            {
                // Convert local → UTC
                dto.ScheduledAt = ToUtcFromLocal(dto.ScheduledAt);
                if (dto.ExpireAt.HasValue)
                    dto.ExpireAt = ToUtcFromLocal(dto.ExpireAt.Value);

                if (dto.CreatedBy <= 0)
                    dto.CreatedBy = GetCurrentUserId();

                // Chuẩn hoá Type/TargetType về schema BLL/DB
                dto.Type = NormalizeTypeForService(dto.Type);
                dto.TargetType = NormalizeTargetTypeForService(dto.TargetType);

                var err = ValidateNotification(dto.Title, dto.Message, dto.Type, dto.TargetType,
                    dto.TargetRoleId, dto.TargetUserId, dto.ScheduledAt, dto.ExpireAt);

                if (!string.IsNullOrEmpty(err))
                    throw new InvalidOperationException(err);

                if (dto.NotificationId.HasValue && dto.NotificationId.Value > 0)
                {
                    // Update
                    await _svc.UpdateAsync(new NotificationUpdateDto
                    {
                        NotificationId = dto.NotificationId.Value,
                        CreatedBy = dto.CreatedBy,
                        Title = dto.Title,
                        Message = dto.Message,
                        Type = dto.Type,
                        TargetType = dto.TargetType,
                        TargetRoleId = dto.TargetRoleId,
                        TargetUserId = dto.TargetUserId,
                        ConditionJson = dto.ConditionJson,
                        ScheduledAt = dto.ScheduledAt,
                        ExpireAt = dto.ExpireAt
                    });

                    TempData["Success"] = "✅ Cập nhật thông báo thành công.";
                }
                else
                {
                    // Create
                    await _svc.CreateAsync(new NotificationCreateDto
                    {
                        CreatedBy = dto.CreatedBy,
                        Title = dto.Title,
                        Message = dto.Message,
                        Type = dto.Type,
                        TargetType = dto.TargetType,
                        TargetRoleId = dto.TargetRoleId,
                        TargetUserId = dto.TargetUserId,
                        ConditionJson = dto.ConditionJson,
                        ScheduledAt = dto.ScheduledAt,
                        ExpireAt = dto.ExpireAt
                    });
                    TempData["Success"] = "✅ Thêm thông báo mới thành công.";
                }

                return RedirectToAction(nameof(Manage), f);
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi trong modal
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = ex.Message;

                var list = await _svc.SearchAsync(f);
                ViewBag.Filter = f;

                // Fill lại form edit
                ViewBag.EditNotification = dto.NotificationId.HasValue
                    ? new NotificationDto
                    {
                        NotificationId = dto.NotificationId.Value,
                        CreatedBy = dto.CreatedBy,
                        Title = dto.Title,
                        Message = dto.Message,
                        Type = dto.Type,
                        TargetType = dto.TargetType,
                        TargetRoleId = dto.TargetRoleId,
                        TargetUserId = dto.TargetUserId,
                        ConditionJson = dto.ConditionJson,
                        ScheduledAt = dto.ScheduledAt,
                        ExpireAt = dto.ExpireAt,
                        IsSent = false,
                        IsCanceled = false,
                        CreatedAt = DateTime.UtcNow
                    }
                    : null;

                return View("~/Views/Admin/ManageNotification.cshtml", list);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id, [FromQuery] NotificationFilterDto f)
        {
            try
            {
                await _svc.DeleteAsync(id);
                TempData["toast"] = "🗑️ Đã xóa thông báo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Manage), NormalizeFilter(f));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, [FromQuery] NotificationFilterDto f)
        {
            try
            {
                await _svc.CancelAsync(id);
                TempData["toast"] = "❌ Đã hủy thông báo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Manage), NormalizeFilter(f));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNow(int id, [FromQuery] NotificationFilterDto f)
        {
            try
            {
                await _svc.SendNowAsync(id);
                TempData["toast"] = "📨 Đã gửi ngay.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Manage), NormalizeFilter(f));
        }

        // =============== Helpers ===============
        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }

        private static NotificationFilterDto NormalizeFilter(NotificationFilterDto f)
        {
            f ??= new NotificationFilterDto();
            if (f.Page <= 0) f.Page = 1;
            if (f.PageSize <= 0) f.PageSize = 20;
            return f;
        }

        private static string ValidateNotification(
            string title, string message, string type, string targetType,
            int? targetRoleId, int? targetUserId, DateTime scheduledUtc, DateTime? expireUtc)
        {
            if (string.IsNullOrWhiteSpace(title)) return "Tiêu đề là bắt buộc.";
            if (string.IsNullOrWhiteSpace(message)) return "Nội dung là bắt buộc.";
            if (string.IsNullOrWhiteSpace(type)) return "Type là bắt buộc.";
            if (string.IsNullOrWhiteSpace(targetType)) return "TargetType là bắt buộc.";

            // targetType đã normalize: All / ByRole / SingleUser / ByCondition
            switch (targetType)
            {
                case "All":
                    if (targetRoleId.HasValue || targetUserId.HasValue)
                        return "Không đặt TargetRoleId/TargetUserId khi TargetType = All.";
                    break;
                case "ByRole":
                    if (!targetRoleId.HasValue)
                        return "TargetRoleId là bắt buộc khi TargetType = ByRole.";
                    break;
                case "SingleUser":
                    if (!targetUserId.HasValue)
                        return "TargetUserId là bắt buộc khi TargetType = SingleUser.";
                    break;
                case "ByCondition":
                    // Cho phép null ConditionJson ở Controller; Service sẽ validate kỹ hơn nếu cần
                    break;
                default:
                    return "TargetType không hợp lệ.";
            }

            if (expireUtc.HasValue && expireUtc.Value <= scheduledUtc)
                return "ExpireAt phải lớn hơn ScheduledAt.";

            return string.Empty;
        }

        private static DateTime ToUtcFromLocal(DateTime local)
        {
            // Nếu DateTime.Kind chưa là Local, ép về Local rồi ToUniversalTime
            var localSpecified = DateTime.SpecifyKind(local, DateTimeKind.Local);
            return localSpecified.ToUniversalTime();
        }

        private static string NormalizeTypeForService(string? type)
        {
            var t = (type ?? "").Trim().ToLowerInvariant();
            return t switch
            {
                "general" or "info" => "General",
                "order" => "Order",
                "promotion" or "promo" => "Promotion",
                "system" => "System",
                _ => "General"
            };
        }

        private static string NormalizeTargetTypeForService(string? targetType)
        {
            var t = (targetType ?? "").Trim().ToLowerInvariant();
            return t switch
            {
                "all" => "All",
                "role" or "byrole" => "ByRole",
                "user" or "singleuser" => "SingleUser",
                "condition" or "bycondition" => "ByCondition",
                _ => "All"
            };
        }
    }
}
