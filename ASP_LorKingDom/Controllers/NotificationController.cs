using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    // [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class NotificationController : Controller
    {
        private readonly INotificationService _svc;
        private readonly IRoleRepository _roleRepo;

        public NotificationController(INotificationService svc, IRoleRepository roleRepository)
        {
            _svc = svc;
            _roleRepo = roleRepository;
        }

        public IActionResult Index() => RedirectToAction(nameof(Manage));

        [HttpGet]
        public async Task<IActionResult> Manage([FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);
            var list = await _svc.SearchAsync(f);
            ViewBag.Filter = f;

            var roles = await _roleRepo.GetAllAsync();
            ViewBag.Roles = roles;

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

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _roleRepo.GetAllAsync();
                return Json(roles.Select(r => new { id = r.RoleId, name = r.RoleName }));
            }
            catch
            {
                return Json(new { error = "Không thể tải danh sách vai trò" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotification(NotificationSaveDto dto, [FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);

            try
            {
                var basicValidationError = ValidateBasicInput(dto);
                if (!string.IsNullOrEmpty(basicValidationError))
                    throw new InvalidOperationException(basicValidationError);

                dto.ScheduledAt = ToUtcFromLocal(dto.ScheduledAt);
                if (dto.ExpireAt.HasValue)
                    dto.ExpireAt = ToUtcFromLocal(dto.ExpireAt.Value);

                if (dto.CreatedBy <= 0)
                    dto.CreatedBy = GetCurrentUserId();

                dto.Type = NormalizeTypeForService(dto.Type);
                dto.TargetType = NormalizeTargetTypeForService(dto.TargetType);

                if (dto.NotificationId.HasValue && dto.NotificationId.Value > 0)
                {
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
                return await HandleSaveError(ex, dto, f);
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

        // =============== Private Helpers ===============

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

        private static string ValidateBasicInput(NotificationSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return "Tiêu đề là bắt buộc.";
            if (string.IsNullOrWhiteSpace(dto.Message)) return "Nội dung là bắt buộc.";
            if (dto.ExpireAt.HasValue && dto.ExpireAt.Value <= dto.ScheduledAt)
                return "Thời gian hết hạn phải sau thời gian hiển thị.";
            return string.Empty;
        }

        private async Task<IActionResult> HandleSaveError(Exception ex, NotificationSaveDto dto, NotificationFilterDto f)
        {
            ViewBag.ShowErrorModal = true;
            ViewBag.ErrorMessage = ex.Message;

            var list = await _svc.SearchAsync(f);
            ViewBag.Filter = f;

            var roles = await _roleRepo.GetAllAsync();
            ViewBag.Roles = roles;

            if (dto.NotificationId.HasValue && dto.NotificationId.Value > 0)
            {
                ViewBag.EditNotification = new NotificationDto
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
                };
            }
            else
            {
                ViewBag.EditNotification = null;
            }

            return View("~/Views/Admin/ManageNotification.cshtml", list);
        }

        private static DateTime ToUtcFromLocal(DateTime local)
        {
            if (local.Kind == DateTimeKind.Utc) return local;
            var localSpecified = DateTime.SpecifyKind(local, DateTimeKind.Local);
            return localSpecified.ToUniversalTime();
        }

        private static string NormalizeTypeForService(string? type) =>
            (type ?? "").Trim().ToLowerInvariant() switch
            {
                "info" => "General",
                "general" => "General",
                "order" => "Order",
                "promo" or "promotion" => "Promotion",
                "warning" or "error" or "system" => "System",
                _ => "General"
            };

        private static string NormalizeTargetTypeForService(string? targetType) =>
            (targetType ?? "").Trim().ToLowerInvariant() switch
            {
                "all" => "All",
                "role" or "byrole" => "ByRole",
                "user" or "singleuser" => "SingleUser",
                "condition" or "bycondition" => "ByCondition",
                _ => "All"
            };
    }
}
