using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly] // Staff: Notification Management
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

        // ===== INDEX - View List (Không filter) =====
        [HttpGet("Notification/Manage")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var f = NormalizeFilter(new NotificationFilterDto { Page = page, PageSize = pageSize });
            var pagedResult = await _svc.SearchAsync(f);

            ViewBag.Filter = f;
            ViewBag.Roles = await _roleRepo.GetAllAsync();

            if (TempData["EditNotificationId"] is int nid && nid > 0)
            {
                ViewBag.EditNotification = await _svc.GetByIdAsync(nid);
            }

            return View("~/Views/Admin/ManageNotification.cshtml", pagedResult);
        }

        // ===== SEARCH - Tìm kiếm với filter =====
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);
            var pagedResult = await _svc.SearchAsync(f);

            ViewBag.Filter = f;
            ViewBag.Roles = await _roleRepo.GetAllAsync();

            if (TempData["EditNotificationId"] is int nid && nid > 0)
            {
                ViewBag.EditNotification = await _svc.GetByIdAsync(nid);
            }

            return View("~/Views/Admin/ManageNotification.cshtml", pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, [FromQuery] NotificationFilterDto f)
        {
            var n = await _svc.GetByIdAsync(id);
            if (n == null)
            {
                TempData["Error"] = "Không tìm thấy thông báo cần sửa.";
                return RedirectToListOrSearch(f);
            }

            TempData["EditNotificationId"] = id;
            return RedirectToListOrSearch(f);
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleRepo.GetAllAsync();
            return Json(roles.Select(r => new { id = r.RoleId, name = r.RoleName }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotification(NotificationSaveDto dto, [FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);

            var validationError = ValidateBasicInput(dto);
            if (!string.IsNullOrWhiteSpace(validationError))
                return await HandleCreateError(new InvalidOperationException(validationError), dto, f);

            PrepareDto(dto);

            try
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

                TempData["Success"] = "Thêm thông báo mới thành công.";
                return RedirectToListOrSearch(f);
            }
            catch (InvalidOperationException ex)
            {
                return await HandleCreateError(ex, dto, f);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var message = dbEx.InnerException?.Message ?? dbEx.Message;
                return await HandleCreateError(new Exception($"Lỗi database: {message}"), dto, f);
            }
            catch (Exception ex)
            {
                return await HandleCreateError(ex, dto, f);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotification(NotificationSaveDto dto, [FromQuery] NotificationFilterDto f)
        {
            f = NormalizeFilter(f);

            if (!dto.NotificationId.HasValue || dto.NotificationId.Value <= 0)
                return await HandleUpdateError(new InvalidOperationException("ID thông báo không hợp lệ."), dto, f);

            var validationError = ValidateBasicInput(dto);
            if (!string.IsNullOrWhiteSpace(validationError))
                return await HandleUpdateError(new InvalidOperationException(validationError), dto, f);

            PrepareDto(dto);

            try
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

                TempData["Success"] = "Cập nhật thông báo thành công.";
                return RedirectToListOrSearch(f);
            }
            catch (KeyNotFoundException ex)
            {
                return await HandleUpdateError(ex, dto, f);
            }
            catch (InvalidOperationException ex)
            {
                return await HandleUpdateError(ex, dto, f);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var message = dbEx.InnerException?.Message ?? dbEx.Message;
                return await HandleUpdateError(new Exception($"Lỗi database: {message}"), dto, f);
            }
            catch (Exception ex)
            {
                return await HandleUpdateError(ex, dto, f);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, [FromQuery] NotificationFilterDto f)
        {
            try
            {
                bool deleted = await _svc.DeleteAsync(id);

                if (deleted)
                    TempData["Success"] = "Đã xóa thông báo thành công.";
                else
                    TempData["Error"] = "Không tìm thấy thông báo để xóa.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                TempData["Error"] = $"Lỗi database khi xóa: {innerMessage}";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = $"Lỗi: {innerMessage}";
            }

            return RedirectToListOrSearch(f);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, [FromQuery] NotificationFilterDto f)
        {
            try
            {
                await _svc.CancelAsync(id);
                TempData["Success"] = "Đã hủy thông báo thành công.";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                TempData["Error"] = $"Lỗi database: {innerMessage}";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = $"Lỗi: {innerMessage}";
            }
            return RedirectToListOrSearch(f);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNow(int id, [FromQuery] NotificationFilterDto f)
        {
            try
            {
                await _svc.SendNowAsync(id);
                TempData["Success"] = "Đã gửi thông báo thành công.";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                TempData["Error"] = $"Lỗi database: {innerMessage}";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = $"Lỗi: {innerMessage}";
            }
            return RedirectToListOrSearch(f);
        }

        // ===== Helpers =====

        /// <summary>
        /// Helper: Redirect về Index hoặc Search tùy theo có keyword hay không
        /// </summary>
        private IActionResult RedirectToListOrSearch(NotificationFilterDto f)
        {
            f = NormalizeFilter(f);

            if (!string.IsNullOrWhiteSpace(f.Keyword))
            {
                return RedirectToAction(nameof(Search), f);
            }

            return RedirectToAction(nameof(Index), new { page = f.Page, pageSize = f.PageSize });
        }

        /// <summary>
        /// Helper: Chuẩn bị DTO trước khi gọi Service (convert time, normalize type, set default user)
        /// </summary>
        private void PrepareDto(NotificationSaveDto dto)
        {
            dto.ScheduledAt = ToUtcFromLocal(dto.ScheduledAt);
            if (dto.ExpireAt.HasValue)
                dto.ExpireAt = ToUtcFromLocal(dto.ExpireAt.Value);

            if (dto.CreatedBy <= 0)
                dto.CreatedBy = GetCurrentUserId();

            dto.Type = NormalizeTypeForService(dto.Type);
            dto.TargetType = NormalizeTargetTypeForService(dto.TargetType);
        }

        /// <summary>
        /// Helper: Lấy UserId từ Claims
        /// </summary>
        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }

        /// <summary>
        /// Helper: Chuẩn hóa filter (đảm bảo Page >= 1, PageSize hợp lệ)
        /// </summary>
        private static NotificationFilterDto NormalizeFilter(NotificationFilterDto f)
        {
            f ??= new NotificationFilterDto();
            if (f.Page <= 0) f.Page = 1;
            if (f.PageSize <= 0) f.PageSize = 20;
            return f;
        }

        /// <summary>
        /// Helper: Validate input cơ bản (Title, Message, ExpireAt)
        /// </summary>
        private static string ValidateBasicInput(NotificationSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return "Tiêu đề là bắt buộc.";
            if (string.IsNullOrWhiteSpace(dto.Message))
                return "Nội dung là bắt buộc.";
            if (dto.ExpireAt.HasValue && dto.ExpireAt.Value <= dto.ScheduledAt)
                return "Thời gian hết hạn phải sau thời gian hiển thị.";
            return string.Empty;
        }

        /// <summary>
        /// Helper: Xử lý lỗi khi Create (hiển thị lại form Add với lỗi)
        /// </summary>
        private async Task<IActionResult> HandleCreateError(Exception ex, NotificationSaveDto dto, NotificationFilterDto f)
        {
            ViewBag.ShowErrorModal = true;
            ViewBag.ErrorMessage = ex.Message;
            ViewBag.ShowAddModal = true;

            var pagedResult = await _svc.SearchAsync(f);
            ViewBag.Filter = f;
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            ViewBag.EditNotification = null;

            return View("~/Views/Admin/ManageNotification.cshtml", pagedResult);
        }

        /// <summary>
        /// Helper: Xử lý lỗi khi Update (hiển thị lại form Edit với lỗi)
        /// </summary>
        private async Task<IActionResult> HandleUpdateError(Exception ex, NotificationSaveDto dto, NotificationFilterDto f)
        {
            ViewBag.ShowErrorModal = true;
            ViewBag.ErrorMessage = ex.Message;

            var pagedResult = await _svc.SearchAsync(f);
            ViewBag.Filter = f;
            ViewBag.Roles = await _roleRepo.GetAllAsync();

            // Restore data để hiển thị lại modal edit
            ViewBag.EditNotification = new NotificationDto
            {
                NotificationId = dto.NotificationId ?? 0,
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

            return View("~/Views/Admin/ManageNotification.cshtml", pagedResult);
        }

        /// <summary>
        /// Helper: Convert DateTime từ Local sang UTC
        /// </summary>
        private static DateTime ToUtcFromLocal(DateTime local)
        {
            if (local.Kind == DateTimeKind.Utc) return local;
            var localSpecified = DateTime.SpecifyKind(local, DateTimeKind.Local);
            return localSpecified.ToUniversalTime();
        }

        /// <summary>
        /// Helper: Normalize Type từ UI sang Service (info → General, promo → Promotion, v.v.)
        /// </summary>
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

        /// <summary>
        /// Helper: Normalize TargetType từ UI sang Service (Role → ByRole, User → SingleUser, v.v.)
        /// </summary>
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
