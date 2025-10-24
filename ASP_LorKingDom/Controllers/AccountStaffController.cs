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

        // ===== MANAGE =====
        [HttpGet]
        public async Task<IActionResult> Manage(string? q)
        {
            await LoadRolesAsync();
            var items = await _accountService.GetAllStaffForAdminAsync();
            items = Filter(items, q);
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountStaff.cshtml", items.OrderByDescending(c => c.CreatedAt).ToList());
        }

        // ===== CREATE =====
        [HttpPost("account-staff/create")]
        public async Task<IActionResult> CreateStaff(
            [FromForm] AccountDto model,
            [FromForm] string? q,
            [FromForm] IFormFile? AvatarFile,
            [FromForm] string? ConfirmPassword)
        {
            try
            {
                await LoadRolesAsync();
                var v = AccountValidator.ValidateCreateStaff(
                    model.AccountName, model.Email, model.PhoneNumber, model.RoleId, model.Password, ConfirmPassword ?? "");

                if (!v.IsValid) AddModelErrors(v.Errors);

                if (model.RoleId.HasValue && model.RoleId.Value > 0)
                {
                    var role = await _roleRepository.GetByIdAsync(model.RoleId.Value);
                    if (role == null) ModelState.AddModelError(nameof(model.RoleId), "Vai trò không tồn tại.");
                }

                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    await _accountService.ExistsByEmailAsync(model.Email))
                    ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(model.PhoneNumber))
                    ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại.");

                if (AvatarFile is { Length: > 0 })
                {
                    var imgV = AccountValidator.ValidateImageFile(AvatarFile.Length, AvatarFile.ContentType);
                    if (!imgV.IsValid) AddModelErrors(imgV.Errors);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = FirstModelStateError();
                    return await ReloadManagePage(q, model);
                }

                model.Id = 0;
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
                        return await ReloadManagePage(q, model);
                    }
                    model.Image = pathOrErr;
                }

                var newId = await _accountService.CreateAsync(model);
                TempData["Success"] = newId > 0 ? "Thêm nhân viên thành công!" : "Thêm nhân viên thất bại!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                return await ReloadManagePage(q, model);
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
            [FromForm] IFormFile? AvatarFile = null)
        {
            try
            {
                await LoadRolesAsync();

                if (Id <= 0)
                {
                    TempData["Error"] = "Thiếu Id nhân viên.";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                var v = AccountValidator.ValidateUpdateStaff(
                    AccountName, Email, PhoneNumber, RoleId, Status, NewPassword ?? "", ConfirmNewPassword ?? "");
                if (!v.IsValid) AddModelErrors(v.Errors);

                if (RoleId.HasValue && RoleId.Value > 0)
                {
                    var role = await _roleRepository.GetByIdAsync(RoleId.Value);
                    if (role == null) ModelState.AddModelError(nameof(RoleId), "Vai trò không tồn tại.");
                }

                if (!string.IsNullOrWhiteSpace(PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(PhoneNumber, Id))
                    ModelState.AddModelError(nameof(PhoneNumber), "Số điện thoại đã tồn tại.");

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
                    return await ReloadManagePage(q, temp);
                }

                var existing = await _accountService.GetByIdAsync(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                existing.AccountName = AccountName?.Trim();
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
                        return await ReloadManagePage(q, existing);
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
                TempData["Success"] = success ? "Cập nhật nhân viên thành công!" : "Cập nhật thất bại!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Dữ liệu đã bị thay đổi bởi người dùng khác. Vui lòng thử lại.";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Manage), new { q });
            }
        }

        // ===== DELETE / RESTORE =====
        [HttpPost]
        public async Task<IActionResult> DeleteStaff(int id, string? q)
        {
            try
            {
                var staff = await _accountService.GetByIdAsync(id);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction(nameof(Manage), new { q });
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
            return RedirectToAction(nameof(Manage), new { q });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreStaff(int id, string? q)
        {
            try
            {
                var staff = await _accountService.GetByIdAsync(id);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction(nameof(Manage), new { q });
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
            return RedirectToAction(nameof(Manage), new { q });
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

        private async Task<IActionResult> ReloadManagePage(string? q, AccountDto? editModel = null)
        {
            await LoadRolesAsync();
            var all = await _accountService.GetAllStaffForAdminAsync();
            all = Filter(all, q);

            if (editModel != null) ViewBag.EditStaff = editModel;
            ViewBag.Query = q;

            return View("~/Views/Admin/ManageAccountStaff.cshtml", all.OrderByDescending(c => c.CreatedAt).ToList());
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
