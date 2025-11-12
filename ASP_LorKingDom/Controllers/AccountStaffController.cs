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
        private readonly IAvatarService _avatarService;

        public AccountStaffController(
            IAccountService accountService, 
            IRoleRepository roleRepo,
          IAvatarService avatarService)
        {
            _accountService = accountService;
            _roleRepository = roleRepo;
            _avatarService = avatarService;
        }

        [HttpGet("AccountStaff/Index")]
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

        [HttpGet("AccountStaff/Search")]
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

        [HttpPost("account-staff/create")]
        public async Task<IActionResult> CreateStaff(
            [Bind("AccountName,Email,PhoneNumber,RoleId,Password,ConfirmPassword,Status,IsDeleted,SearchQuery,PageSize")] BLL.DTOs.StaffFormModel model,
            IFormFile? AvatarFile)
        {
            await LoadRolesAsync();
            model.Id = 0;
            
            // 1) Xử lý avatar (nếu có)
            string? imagePath = null;
            if (AvatarFile is { Length: > 0 })
            {
                var (success, pathOrError) = await _avatarService.SaveAvatarAsync(AvatarFile);
                if (!success)
                {
                    var errorDto = model.ToAccountDto();
                    return await ShowAddValidationError(pathOrError, errorDto, model.SearchQuery, model.PageSize);
                }
                imagePath = pathOrError;
            }

            // 2) Chuyển sang AccountDto
            var accountDto = model.ToAccountDto();
            accountDto.Image = imagePath;
            accountDto.Status = model.IsDeleted ? "Inactive" : "Active";
            accountDto.CreatedAt = DateTime.Now;

            try
            {
                // 3) Service sẽ validate và create
                var newId = await _accountService.CreateAsync(accountDto);
                
                TempData["Success"] = newId > 0 
                    ? "Thêm nhân viên thành công!" 
                    : "Thêm nhân viên thất bại!";
                
                return RedirectToListOrSearch(model.SearchQuery, 1, model.PageSize);
            }
            catch (InvalidOperationException ex)
            {
                // Validation errors từ Service (email trùng, phone trùng, etc.)
                // CRITICAL: Phải giữ lại password để restore form
                accountDto.Password = model.Password;
                accountDto.Image = imagePath;
                
                // Xóa avatar nếu upload thất bại
                if (!string.IsNullOrWhiteSpace(imagePath))
                    _avatarService.DeleteAvatar(imagePath);
                    
                return await ShowAddValidationError(ex.Message, accountDto, model.SearchQuery, model.PageSize);
            }
            catch (Exception ex)
            {
                // Unexpected errors
                accountDto.Password = model.Password;
                accountDto.Image = imagePath;
                
                // Xóa avatar nếu upload thất bại
                if (!string.IsNullOrWhiteSpace(imagePath))
                    _avatarService.DeleteAvatar(imagePath);
                    
                return await ShowAddValidationError($"Lỗi: {ex.Message}", accountDto, model.SearchQuery, model.PageSize);
            }
        }

        // ===== UPDATE =====
        [HttpPost("account-staff/update")]
        public async Task<IActionResult> UpdateStaff(
            [Bind("Id,AccountName,Email,PhoneNumber,RoleId,NewPassword,ConfirmNewPassword,Status,IsDeleted,ExistingImage,RemoveImage,SearchQuery,PageSize")] BLL.DTOs.StaffFormModel model,
            IFormFile? AvatarFile)
        {
            await LoadRolesAsync();

            // 1) Validate Id
            if (model.Id <= 0)
            {
                TempData["Error"] = "ID nhân viên không hợp lệ.";
                return RedirectToListOrSearch(model.SearchQuery, 1, model.PageSize);
            }
            
            // 2) Kiểm tra staff có tồn tại
            var existing = await _accountService.GetByIdAsync(model.Id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToListOrSearch(model.SearchQuery, 1, model.PageSize);
            }

            // 3) Xử lý avatar
            string? newImagePath = model.ExistingImage;
            
            if (AvatarFile is { Length: > 0 })
            {
                // Upload avatar mới
                var (success, pathOrError) = await _avatarService.SaveAvatarAsync(AvatarFile);
                if (!success)
                {
                    var tempDto = model.ToAccountDto();
                    tempDto.Image = model.ExistingImage;
                    return await ShowEditValidationError(pathOrError, tempDto, model.SearchQuery, model.PageSize);
                }
                
                // Xóa avatar cũ
                _avatarService.DeleteAvatar(existing.Image);
                newImagePath = pathOrError;
            }
            else if (model.RemoveImage)
            {
                // Xóa avatar
                _avatarService.DeleteAvatar(existing.Image);
                newImagePath = null;
            }

            // 4) Chuyển sang DTO và update
            var accountDto = model.ToAccountDto();
            accountDto.Id = model.Id;
            accountDto.Email = existing.Email; // Không cho phép đổi email
            accountDto.Image = newImagePath;
            accountDto.Status = model.IsDeleted ? "Inactive" : model.Status;
            accountDto.Password = !string.IsNullOrWhiteSpace(model.NewPassword) ? model.NewPassword : existing.Password;
            accountDto.CreatedAt = existing.CreatedAt;

            try
            {
                // 5) Service validate và update
                var success = await _accountService.UpdateAsync(model.Id, accountDto);
                
                TempData["Success"] = success 
                    ? "Cập nhật nhân viên thành công!" 
                    : "Cập nhật thất bại!";
                
                return RedirectToListOrSearch(model.SearchQuery, 1, model.PageSize);
            }
            catch (InvalidOperationException ex)
            {
                // Validation errors (phone trùng, email change attempt, etc.)
                return await ShowEditValidationError(ex.Message, accountDto, model.SearchQuery, model.PageSize);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = $"{ex.Message}";
                return RedirectToListOrSearch(model.SearchQuery, 1, model.PageSize);
            }
            catch (Exception ex)
            {
                return await ShowEditValidationError($"❌ Lỗi: {ex.Message}", accountDto, model.SearchQuery, model.PageSize);
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
                Page = page,      
                PageSize = pageSize,
                Total = totalItems  
            };
        }

        private async Task<IActionResult> ReloadManagePage(string? q, int page, int pageSize, AccountDto? editModel = null)
        {
            await LoadRolesAsync();
            var all = await _accountService.GetAllStaffForAdminAsync();
            var filtered = Filter(all, q);
            var ordered = filtered.OrderByDescending(c => c.CreatedAt).ToList();
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

        // ===== Helper Methods =====
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