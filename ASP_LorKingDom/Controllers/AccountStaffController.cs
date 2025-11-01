using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebUI.Controllers
{
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
        [HttpGet]
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

        // ===== MANAGE - Xử lý cả list và search =====
        [HttpGet]
        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 10)
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
            try
            {
                await LoadRolesAsync();
                
                // CRITICAL: Force Id = 0 để đảm bảo đây là Create, không phải Update
                model.Id = 0;
                
                var v = AccountValidator.ValidateCreateStaff(
                    model.AccountName, model.Email, model.PhoneNumber ?? "", model.RoleId, model.Password ?? "", ConfirmPassword ?? "");

                if (!v.IsValid) AddModelErrors(v.Errors);

                if (model.RoleId.HasValue && model.RoleId.Value > 0)
                {
                    var role = await _roleRepository.GetByIdAsync(model.RoleId.Value);
                    if (role == null) ModelState.AddModelError(nameof(model.RoleId), "Vai trò không tồn tại.");
                }

                // Kiểm tra email đã tồn tại - KHÔNG cho phép trùng khi tạo mới
                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    await _accountService.ExistsByEmailAsync(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại trong hệ thống.");
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Email đã tồn tại trong hệ thống.";
                    ViewBag.ShowAddModal = true;
                    return await ReloadManagePage(q, 1, pageSize, model);
                }
                
                // Kiểm tra phone đã tồn tại - KHÔNG cho phép trùng khi tạo mới
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(model.PhoneNumber))
                {
                    ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại trong hệ thống.");
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Số điện thoại đã tồn tại trong hệ thống.";
                    ViewBag.ShowAddModal = true;
                    return await ReloadManagePage(q, 1, pageSize, model);
                }

                if (AvatarFile is { Length: > 0 })
                {
                    var imgV = AccountValidator.ValidateImageFile(AvatarFile.Length, AvatarFile.ContentType);
                    if (!imgV.IsValid) AddModelErrors(imgV.Errors);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = FirstModelStateError();
                    ViewBag.ShowAddModal = true;
                    ViewBag.AddStaffModel = model;
                    return await ReloadManagePage(q, 1, pageSize, model);
                }

                model.Status = model.IsDeleted ? "Inactive" : "Active";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = null;

                if (AvatarFile is { Length: > 0 })
                {
                    var (ok, pathOrErr) = await TrySaveAvatarAsync(AvatarFile);
                    if (!ok)
                    {
                        ViewBag.ShowErrorModal = true;
                        ViewBag.ErrorMessage = pathOrErr;
                        ViewBag.ShowAddModal = true;
                        ViewBag.AddStaffModel = model;
                        return await ReloadManagePage(q, 1, pageSize, model);
                    }
                    model.Image = pathOrErr;
                }

                var newId = await _accountService.CreateAsync(model);
                
                if (newId > 0)
                {
                    TempData["Success"] = "✅ Thêm nhân viên thành công!";
                }
                else
                {
                    TempData["Error"] = "❌ Thêm nhân viên thất bại! Vui lòng thử lại.";
                }
                
                // Redirect về Search nếu có query, về Index nếu không
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
            catch (Exception ex)
            {
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                ViewBag.ShowAddModal = true;
                ViewBag.AddStaffModel = model;
                return await ReloadManagePage(q, 1, pageSize, model);
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
            try
            {
                await LoadRolesAsync();

                // CRITICAL: Id phải > 0 để đảm bảo đây là Update, không phải Create
                if (Id <= 0)
                {
                    TempData["Error"] = "❌ ID nhân viên không hợp lệ. Không thể cập nhật.";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }
                
                // Kiểm tra staff có tồn tại không
                var existing = await _accountService.GetByIdAsync(Id);
                if (existing == null)
                {
                    TempData["Error"] = "❌ Không tìm thấy nhân viên cần cập nhật!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                var v = AccountValidator.ValidateUpdateStaff(
                    AccountName, Email, PhoneNumber ?? "", RoleId, Status, NewPassword ?? "", ConfirmNewPassword ?? "");
                if (!v.IsValid) AddModelErrors(v.Errors);

                if (RoleId.HasValue && RoleId.Value > 0)
                {
                    var role = await _roleRepository.GetByIdAsync(RoleId.Value);
                    if (role == null) ModelState.AddModelError(nameof(RoleId), "Vai trò không tồn tại.");
                }

                // Kiểm tra phone trùng - loại trừ chính staff hiện tại
                if (!string.IsNullOrWhiteSpace(PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(PhoneNumber, Id))
                {
                    ModelState.AddModelError(nameof(PhoneNumber), "Số điện thoại đã được sử dụng bởi nhân viên khác.");
                }

                if (AvatarFile is { Length: > 0 })
                {
                    var imgV = AccountValidator.ValidateImageFile(AvatarFile.Length, AvatarFile.ContentType);
                    if (!imgV.IsValid) AddModelErrors(imgV.Errors);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = FirstModelStateError();
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
                    ViewBag.EditStaff = temp;
                    return await ReloadManagePage(q, 1, pageSize, temp);
                }

                // Update existing staff info
                existing.AccountName = AccountName?.Trim() ?? existing.AccountName;
                existing.PhoneNumber = PhoneNumber;
                existing.RoleId = RoleId;
                existing.IsDeleted = IsDeleted;
                existing.Status = IsDeleted ? "Inactive" : Status;
                existing.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(NewPassword))
                    existing.Password = NewPassword; // Service sẽ hash khi UpdateAsync

                if (AvatarFile is { Length: > 0 })
                {
                    var (ok, pathOrErr) = await TrySaveAvatarAsync(AvatarFile);
                    if (!ok)
                    {
                        ViewBag.ShowErrorModal = true;
                        ViewBag.ErrorMessage = pathOrErr;
                        ViewBag.EditStaff = existing;
                        return await ReloadManagePage(q, 1, pageSize, existing);
                    }
                    DeleteOldAvatar(existing.Image);
                    existing.Image = pathOrErr;
                }
                else if (RemoveImage)
                {
                    DeleteOldAvatar(existing.Image);
                    existing.Image = null;
                }

                var success = await _accountService.UpdateAsync(existing.Id, existing);
                TempData["Success"] = success ? "✅ Cập nhật nhân viên thành công!" : "❌ Cập nhật thất bại!";
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "⚠️ Dữ liệu đã bị thay đổi bởi người dùng khác. Vui lòng thử lại.";
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi: {ex.Message}";
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
        }

        // ===== DELETE / RESTORE =====
        [HttpPost]
        public async Task<IActionResult> DeleteStaff(int id, string? q, int pageSize = 10)
        {
            try
            {
                var staff = await _accountService.GetByIdAsync(id);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }
                staff.IsDeleted = true;
                staff.Status = "Inactive";
                staff.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, staff);
                TempData["Success"] = success ? "Xoá nhân viên thành công!" : "Xoá nhân viên thất bại!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xoá: {ex.Message}";
            }
            return string.IsNullOrWhiteSpace(q) 
                ? RedirectToAction(nameof(Index), new { pageSize }) 
                : RedirectToAction(nameof(Search), new { q, pageSize });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreStaff(int id, string? q, int pageSize = 10)
        {
            try
            {
                var staff = await _accountService.GetByIdAsync(id);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                staff.IsDeleted = false;
                staff.Status = "Active";
                staff.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, staff);
                TempData["Success"] = success ? "Khôi phục nhân viên thành công!" : "Khôi phục thất bại!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khôi phục: {ex.Message}";
            }
            return string.IsNullOrWhiteSpace(q) 
                ? RedirectToAction(nameof(Index), new { pageSize }) 
                : RedirectToAction(nameof(Search), new { q, pageSize });
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

        private void AddModelErrors(IEnumerable<string> errors)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
        }

        private string FirstModelStateError()
        {
            return string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct());
        }
    }
}