using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebUI.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountStaffController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IRoleRepository _roleRepository;

        public AccountStaffController(IAccountService accountService, IRoleRepository roleRepo)
        {
            _accountService = accountService;
            _roleRepository = roleRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Manage(string? q)
        {
            await LoadRolesAsync();
            var all = await _accountService.GetAllStaffForAdminAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLowerInvariant();
                all = all.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountStaff.cshtml", all.OrderByDescending(c => c.CreatedAt).ToList());
        }

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

                // Validate
                if (string.IsNullOrWhiteSpace(model.AccountName))
                    ModelState.AddModelError(nameof(model.AccountName), "Tên nhân viên bắt buộc.");
                if (string.IsNullOrWhiteSpace(model.Email))
                    ModelState.AddModelError(nameof(model.Email), "Email bắt buộc.");
                if (!model.RoleId.HasValue || model.RoleId.Value <= 0)
                    ModelState.AddModelError(nameof(model.RoleId), "Vui lòng chọn vai trò.");
                if (string.IsNullOrWhiteSpace(model.Password))
                    ModelState.AddModelError(nameof(model.Password), "Mật khẩu bắt buộc.");
                else if (model.Password.Length < 6)
                    ModelState.AddModelError(nameof(model.Password), "Mật khẩu tối thiểu 6 ký tự.");
                if (!string.Equals(model.Password, ConfirmPassword, StringComparison.Ordinal))
                    ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp.");

                // Email/Phone unique
                if (!string.IsNullOrWhiteSpace(model.Email) && await _accountService.ExistsByEmailAsync(model.Email))
                    ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(model.PhoneNumber))
                    ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại.");

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return await ReloadManagePage(q, model);
                }

                // Defaults
                model.Id = 0;
                model.Status = model.IsDeleted ? "Inactive" : "Active";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = null;

                // Ảnh
                if (AvatarFile is { Length: > 0 })
                {
                    var (ok, msg) = await SaveAvatarAsync(AvatarFile);
                    if (!ok)
                    {
                        ViewBag.ShowErrorModal = true;
                        ViewBag.ErrorMessage = msg;
                        return await ReloadManagePage(q, model);
                    }
                    model.Image = msg;
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

        [HttpPost("account-staff/update")]
        public async Task<IActionResult> UpdateStaff(
            [FromForm] int Id,
            [FromForm] string AccountName,
            [FromForm] string? PhoneNumber,
            [FromForm] int? RoleId,
            [FromForm] string Email,
            [FromForm] string Status,
            [FromForm] bool IsDeleted,
            [FromForm] string? NewPassword,           // ← MẬT KHẨU MỚI (optional)
            [FromForm] string? ConfirmNewPassword,    // ← XÁC NHẬN (optional)
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
                if (string.IsNullOrWhiteSpace(AccountName))
                    ModelState.AddModelError(nameof(AccountName), "Tên nhân viên bắt buộc.");
                if (!RoleId.HasValue || RoleId.Value <= 0)
                    ModelState.AddModelError(nameof(RoleId), "Vui lòng chọn vai trò.");

                // Validate password nếu có nhập
                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    if (NewPassword.Length < 6)
                        ModelState.AddModelError(nameof(NewPassword), "Mật khẩu tối thiểu 6 ký tự.");
                    if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
                        ModelState.AddModelError(nameof(ConfirmNewPassword), "Xác nhận mật khẩu không khớp.");
                }

                // Unique phone
                if (!string.IsNullOrWhiteSpace(PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(PhoneNumber, Id))
                    ModelState.AddModelError(nameof(PhoneNumber), "Số điện thoại đã tồn tại.");

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                    // Tạo model để prefill lại
                    var tempModel = new AccountDto
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
                    ViewBag.EditStaff = tempModel;
                    return await ReloadManagePage(q, tempModel);
                }

                var existing = await _accountService.GetByIdAsync(Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                // Update fields
                existing.AccountName = AccountName;
                existing.PhoneNumber = PhoneNumber;
                existing.RoleId = RoleId;
                existing.IsDeleted = IsDeleted;
                existing.Status = IsDeleted ? "Inactive" : Status;
                existing.UpdatedAt = DateTime.Now;

                // ✅ CHỈ update password nếu có nhập mới
                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    existing.Password = NewPassword; // Service sẽ hash lại
                }

                // Avatar
                if (AvatarFile is { Length: > 0 })
                {
                    var (ok, msg) = await SaveAvatarAsync(AvatarFile);
                    if (!ok)
                    {
                        ViewBag.ShowErrorModal = true;
                        ViewBag.ErrorMessage = msg;
                        var tempModel = new AccountDto
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
                        ViewBag.EditStaff = tempModel;
                        return await ReloadManagePage(q, tempModel);
                    }
                    if (!string.IsNullOrEmpty(existing.Image)) DeleteOldAvatar(existing.Image);
                    existing.Image = msg;
                }
                else if (RemoveImage)
                {
                    if (!string.IsNullOrEmpty(existing.Image)) DeleteOldAvatar(existing.Image);
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
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Manage), new { q });
            }
        }

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
        private async Task<IActionResult> ReloadManagePage(string? q, AccountDto? editModel = null)
        {
            await LoadRolesAsync();
            var all = await _accountService.GetAllStaffForAdminAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLowerInvariant();
                all = all.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

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

        private async Task<(bool ok, string msg)> SaveAvatarAsync(IFormFile file)
        {
            var okTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif" };
            const long MAX = 2 * 1024 * 1024;

            if (!okTypes.Contains(file.ContentType.ToLower())) return (false, "Định dạng ảnh không hợp lệ. Hỗ trợ PNG/JPG/WebP/GIF.");
            if (file.Length > MAX) return (false, "Kích thước ảnh tối đa 2MB.");
            if (file.Length == 0) return (false, "File ảnh rỗng.");

            try
            {
                var uploads = Path.Combine(GetWebRoot(), "uploads", "staff");
                Directory.CreateDirectory(uploads);

                var ext = Path.GetExtension(file.FileName).ToLower();
                var name = $"{Guid.NewGuid():N}{ext}";
                using var stream = System.IO.File.Create(Path.Combine(uploads, name));
                await file.CopyToAsync(stream);

                return (true, $"/uploads/staff/{name}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi lưu file: {ex.Message}");
            }
        }

        private void DeleteOldAvatar(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            try
            {
                var fileName = Path.GetFileName(imagePath);
                var fullPath = Path.Combine(GetWebRoot(), "uploads", "staff", fileName);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch { /* ignore */ }
        }
    }
}