using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminOnly] // Only Admin: Staff Management
    [AutoValidateAntiforgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
    public class AccountStaffController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IRoleRepository _roleRepository;

        public AccountStaffController(IAccountService accountService, IRoleRepository roleRepo)
        {
            _accountService = accountService;
            _roleRepository = roleRepo;
        }

        // ===== INDEX - View List (Không filter) =====
            [HttpGet("AccountStaff/Manage")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            await LoadRolesAsync();
            var allItems = await _accountService.GetAllStaffForAdminAsync();
            var orderedItems = allItems.OrderByDescending(c => c.CreatedAt).ToList();

            var pagedResult = GetPagedResult(orderedItems, page, pageSize);
            ViewBag.Query = null;

            return View("~/Views/Admin/ManageAccountStaff.cshtml", pagedResult);
        }

        // ===== SEARCH - Tìm kiếm với query =====
        [HttpGet]
        public async Task<IActionResult> Search(string? q, int page = 1, int pageSize = 10)
        {
            await LoadRolesAsync();
            var allItems = await _accountService.GetAllStaffForAdminAsync();
            var filteredItems = Filter(allItems, q);
            var orderedItems = filteredItems.OrderByDescending(c => c.CreatedAt).ToList();

            var pagedResult = GetPagedResult(orderedItems, page, pageSize);
            ViewBag.Query = q;

            return View("~/Views/Admin/ManageAccountStaff.cshtml", pagedResult);
        }

        // ===== CREATE =====
        [HttpPost("account-staff/create")]
        public async Task<IActionResult> CreateStaff(
            [FromForm] AccountDto model,
            [FromForm] string? q,
            [FromForm] IFormFile? AvatarFile,
            [FromForm] string? ConfirmPassword,
            [FromForm] int pageSize = 10)
        {
            await LoadRolesAsync();
            
            // CRITICAL: Force Id = 0 để đảm bảo đây là Create, không phải Update
            model.Id = 0;
            
            // Validate image file nếu có
            if (AvatarFile is { Length: > 0 })
            {
                var imgV = AccountValidator.ValidateImageFile(AvatarFile.Length, AvatarFile.ContentType);
                if (!imgV.IsValid)
                {
                    return await ShowAddValidationError(string.Join("; ", imgV.Errors), model, q, pageSize);
                }
                
                var (ok, pathOrErr) = await TrySaveAvatarAsync(AvatarFile);
                if (!ok)
                {
                    return await ShowAddValidationError(pathOrErr, model, q, pageSize);
                }
                model.Image = pathOrErr;
            }

            // Set default values
            model.Status = model.IsDeleted ? "Inactive" : "Active";
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = null;

            try
            {
                // Service sẽ validate tất cả: format, role exists, email unique, phone unique
                var newId = await _accountService.CreateAsync(model);
                
                TempData["Success"] = newId > 0 
                    ? "Thêm nhân viên thành công!" 
                    : "Thêm nhân viên thất bại! Vui lòng thử lại.";
                
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            catch (InvalidOperationException ex)
            {
                // Validation errors từ Service
                return await ShowAddValidationError(ex.Message, model, q, pageSize);
            }
            catch (Exception ex)
            {
                return await ShowAddValidationError($"Lỗi: {ex.Message}", model, q, pageSize);
            }
        }

        // ===== UPDATE =====
        [HttpPost("account-staff/update")]
        public async Task<IActionResult> UpdateStaff(
            [FromForm] int Id,
            [FromForm] string AccountName,
            [FromForm] string? PhoneNumber,
            [FromForm] int? RoleId,
            [FromForm] string Email,
            [FromForm] string Status,
            [FromForm] bool IsDeleted,
            [FromForm] string? NewPassword,
            [FromForm] string? ConfirmNewPassword,
            [FromForm] string? q,
            [FromForm] string? ExistingImage,
            [FromForm] bool RemoveImage = false,
            [FromForm] IFormFile? AvatarFile = null,
            [FromForm] int pageSize = 10)
        {
            await LoadRolesAsync();

            // CRITICAL: Id phải > 0 để đảm bảo đây là Update, không phải Create
            if (Id <= 0)
            {
                TempData["Error"] = "ID nhân viên không hợp lệ. Không thể cập nhật.";
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            
            // Kiểm tra staff có tồn tại không
            var existing = await _accountService.GetByIdAsync(Id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần cập nhật!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }

            // Validate image file nếu có
            if (AvatarFile is { Length: > 0 })
            {
                var imgV = AccountValidator.ValidateImageFile(AvatarFile.Length, AvatarFile.ContentType);
                if (!imgV.IsValid)
                {
                    var temp = new AccountDto
                    {
                        Id = Id,
                        AccountName = AccountName,
                        PhoneNumber = PhoneNumber,
                        RoleId = RoleId,
                        Email = Email,
                        Status = Status,
                        IsDeleted = IsDeleted,
                        Image = ExistingImage
                    };
                    return await ShowEditValidationError(string.Join("; ", imgV.Errors), temp, q, pageSize);
                }
                
                var (ok, pathOrErr) = await TrySaveAvatarAsync(AvatarFile);
                if (!ok)
                {
                    var temp = new AccountDto
                    {
                        Id = Id,
                        AccountName = AccountName,
                        PhoneNumber = PhoneNumber,
                        RoleId = RoleId,
                        Email = Email,
                        Status = Status,
                        IsDeleted = IsDeleted,
                        Image = ExistingImage
                    };
                    return await ShowEditValidationError(pathOrErr, temp, q, pageSize);
                }
                DeleteOldAvatar(existing.Image);
                existing.Image = pathOrErr;
            }
            else if (RemoveImage)
            {
                DeleteOldAvatar(existing.Image);
                existing.Image = null;
            }

            // Update existing staff info
            existing.AccountName = AccountName?.Trim() ?? existing.AccountName;
            existing.PhoneNumber = PhoneNumber;
            existing.RoleId = RoleId;
            existing.IsDeleted = IsDeleted;
            existing.Status = IsDeleted ? "Inactive" : Status;
            existing.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(NewPassword))
                existing.Password = NewPassword; // Service sẽ hash và validate

            try
            {
                // Service sẽ validate tất cả: format, role exists, phone unique
                var success = await _accountService.UpdateAsync(existing.Id, existing);
                TempData["Success"] = success ? "Cập nhật nhân viên thành công!" : "Cập nhật thất bại!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            catch (InvalidOperationException ex)
            {
                // Validation errors từ Service
                return await ShowEditValidationError(ex.Message, existing, q, pageSize);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            catch (Exception ex)
            {
                return await ShowEditValidationError($"Lỗi: {ex.Message}", existing, q, pageSize);
            }
        }

        // ===== DELETE / RESTORE =====
        [HttpPost]
        public async Task<IActionResult> DeleteStaff(int id, string? q, int pageSize = 10)
        {
            var staff = await _accountService.GetByIdAsync(id);
            if (staff == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            
            staff.IsDeleted = true;
            staff.Status = "Inactive";
            staff.UpdatedAt = DateTime.Now;

            var success = await _accountService.UpdateAsync(id, staff);
            TempData["Success"] = success ? "Xoá nhân viên thành công!" : "Xoá nhân viên thất bại!";
            
            return RedirectToListOrSearch(q, 1, pageSize);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreStaff(int id, string? q, int pageSize = 10)
        {
            var staff = await _accountService.GetByIdAsync(id);
            if (staff == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }

            staff.IsDeleted = false;
            staff.Status = "Active";
            staff.UpdatedAt = DateTime.Now;

            var success = await _accountService.UpdateAsync(id, staff);
            TempData["Success"] = success ? "Khôi phục nhân viên thành công!" : "Khôi phục thất bại!";
            
            return RedirectToListOrSearch(q, 1, pageSize);
        }

        // ===== Helpers =====
        private static List<AccountDto> Filter(List<AccountDto> all, string? q)
        {
            if (string.IsNullOrWhiteSpace(q)) return all;
            var kw = q.Trim().ToLowerInvariant();
            return all.Where(c =>
                (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                (c.Email?.ToLower().Contains(kw) ?? false) ||
                (c.PhoneNumber?.Contains(kw) ?? false)).ToList();
        }

        private PagedResult<AccountDto> GetPagedResult(List<AccountDto> items, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var totalItems = items.Count;
            var pagedItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<AccountDto>
            {
                Items = pagedItems,
                Page = page,           // ✅ Dùng Page thay vì CurrentPage
                PageSize = pageSize,
                Total = totalItems     // ✅ Dùng Total thay vì TotalItems
            };
        }

        private async Task<IActionResult> ReloadManagePage(string? q, int page, int pageSize, AccountDto? editModel = null)
        {
            await LoadRolesAsync();
            var all = await _accountService.GetAllStaffForAdminAsync();
            var filtered = Filter(all, q);
            var ordered = filtered.OrderByDescending(c => c.CreatedAt).ToList();

            // ✅ Trả về PagedResult thay vì List
            var pagedResult = GetPagedResult(ordered, page, pageSize);

            if (editModel != null) ViewBag.EditStaff = editModel;
            ViewBag.Query = q;

            return View("~/Views/Admin/ManageAccountStaff.cshtml", pagedResult);
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            ViewBag.Roles = roles.OrderBy(r => r.RoleName).Select(r => new { r.RoleId, r.RoleName }).ToList();
        }

        private static string GetWebRoot() => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        private async Task<(bool ok, string msg)> TrySaveAvatarAsync(IFormFile file)
        {
            var okTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif" };
            const long MAX = 2 * 1024 * 1024;

            if (!okTypes.Contains(file.ContentType.ToLower())) return (false, "Định dạng ảnh không hợp lệ. Hỗ trợ PNG/JPG/WebP/GIF.");
            if (file.Length == 0) return (false, "File ảnh rỗng.");
            if (file.Length > MAX) return (false, "Kích thước ảnh tối đa 2MB.");

            try
            {
                var uploads = Path.Combine(GetWebRoot(), "uploads", "staff");
                Directory.CreateDirectory(uploads);

                var ext = Path.GetExtension(file.FileName).ToLower();
                var name = $"{Guid.NewGuid():N}{ext}";

                await using var stream = System.IO.File.Create(Path.Combine(uploads, name));
                await file.CopyToAsync(stream);

                return (true, $"/uploads/staff/{name}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi lưu file: {ex.Message}");
            }
        }

        private void DeleteOldAvatar(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            try
            {
                var fileName = Path.GetFileName(imagePath);
                if (fileName.Contains("..") || Path.IsPathRooted(fileName)) return;

                var fullPath = Path.Combine(GetWebRoot(), "uploads", "staff", fileName);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch { /* ignore */ }
        }

        // Helper method to avoid repeated redirect logic
        private IActionResult RedirectToListOrSearch(string? q, int page, int pageSize)
        {
            return string.IsNullOrWhiteSpace(q)
                ? RedirectToAction(nameof(Index), new { page, pageSize })
                : RedirectToAction(nameof(Search), new { q, page, pageSize });
        }

        // Helper method for Add validation error display
        private async Task<IActionResult> ShowAddValidationError(string errorMessage, AccountDto model, string? q, int pageSize)
        {
            ViewBag.ShowErrorModal = true;
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.ShowAddModal = true;
            ViewBag.AddStaffModel = model;
            return await ReloadManagePage(q, 1, pageSize, model);
        }

        // Helper method for Edit validation error display
        private async Task<IActionResult> ShowEditValidationError(string errorMessage, AccountDto model, string? q, int pageSize)
        {
            ViewBag.ShowErrorModal = true;
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.EditStaff = model;
            return await ReloadManagePage(q, 1, pageSize, model);
        }
    }
}