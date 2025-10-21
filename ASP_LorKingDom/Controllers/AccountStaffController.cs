using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountStaffController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IWebHostEnvironment _env;
        private readonly IRoleRepository _roleRepository;

        public AccountStaffController(
            IAccountService accountService,
            IWebHostEnvironment env,
            IRoleRepository roleRepo)
        {
            _accountService = accountService;
            _env = env;
            _roleRepository = roleRepo;
        }

        // ===================== MANAGE STAFF =====================
        [HttpGet]
        public async Task<IActionResult> Manage(string? q)
        {
            await LoadRolesAsync(); // load roles cho view

            var allStaff = await _accountService.GetAllStaffAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim().ToLowerInvariant();
                allStaff = allStaff.Where(c =>
                    (c.AccountName?.ToLower().Contains(keyword) ?? false) ||
                    (c.Email?.ToLower().Contains(keyword) ?? false) ||
                    (c.PhoneNumber?.Contains(keyword) ?? false)
                ).ToList();
            }

            var staffDtos = allStaff.OrderByDescending(c => c.CreatedAt).ToList();
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountStaff.cshtml", staffDtos);
        }

        // ===================== DETAILS =====================
        [HttpGet]
        public async Task<IActionResult> Details(int id, string? q)
        {
            var staff = await _accountService.GetByIdAsync(id);
            if (staff == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            await LoadRolesAsync();
            ViewBag.Query = q;
            return View(staff);
        }

        // ===================== EDIT =====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? q)
        {
            var staff = await _accountService.GetByIdAsync(id);
            if (staff == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên!";
                return RedirectToAction(nameof(Manage), new { q });
            }

            await LoadRolesAsync();
            ViewBag.Query = q;
            ViewBag.EditStaff = staff;

            var allStaff = await _accountService.GetAllStaffAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLowerInvariant();
                allStaff = allStaff.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            return View("~/Views/Admin/ManageAccountStaff.cshtml",
                allStaff.OrderByDescending(c => c.CreatedAt).ToList());
        }

        // ===================== CREATE STAFF =====================
        // Dùng route riêng để tránh trùng khớp
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

                // VALIDATE cơ bản
                if (string.IsNullOrWhiteSpace(model.AccountName))
                    ModelState.AddModelError(nameof(model.AccountName), "Tên nhân viên bắt buộc.");
                if (string.IsNullOrWhiteSpace(model.Email))
                    ModelState.AddModelError(nameof(model.Email), "Email bắt buộc.");
                if (!model.RoleId.HasValue || model.RoleId.Value <= 0)
                    ModelState.AddModelError(nameof(model.RoleId), "Vui lòng chọn vai trò.");
                else if (!await _roleRepository.ExistsAsync(model.RoleId.Value))
                    ModelState.AddModelError(nameof(model.RoleId), "Role không hợp lệ.");

                if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
                    ModelState.AddModelError(nameof(model.Password), "Mật khẩu tối thiểu 6 ký tự.");
                if (!string.Equals(model.Password, ConfirmPassword))
                    ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp.");

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Vui lòng kiểm tra lại thông tin.";
                    ViewBag.EditStaff = model;
                    ViewBag.Query = q;
                    return await ReloadManagePage(q, model);
                }

                // Email duy nhất
                if (await _accountService.ExistsByEmailAsync(model.Email!))
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Email đã tồn tại.";
                    ViewBag.EditStaff = model;
                    ViewBag.Query = q;
                    return await ReloadManagePage(q, model);
                }

                // Thiết lập mặc định
                model.Id = 0;
                model.Status = string.IsNullOrWhiteSpace(model.Status) ? "Active" : model.Status;
                model.IsDeleted = false;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = null;

                // Ảnh
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var (ok, pathOrErr) = await SaveAvatarAsync(AvatarFile);
                    if (!ok)
                    {
                        ViewBag.ShowErrorModal = true;
                        ViewBag.ErrorMessage = pathOrErr;
                        ViewBag.EditStaff = model;
                        ViewBag.Query = q;
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
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                ViewBag.EditStaff = model;
                ViewBag.Query = q;
                return await ReloadManagePage(q, model);
            }
        }

        // ===================== UPDATE STAFF =====================
        [HttpPost("account-staff/update")]
        public async Task<IActionResult> UpdateStaff(
            [FromForm] AccountDto model,
            [FromForm] string? q,
            [FromForm] string? ExistingImage,
            [FromForm] bool RemoveImage = false,
            [FromForm] IFormFile? AvatarFile = null
        )
        {
            try
            {
                await LoadRolesAsync();

                if (model.Id <= 0)
                {
                    TempData["Error"] = "Thiếu Id nhân viên.";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                if (string.IsNullOrWhiteSpace(model.AccountName))
                    ModelState.AddModelError(nameof(model.AccountName), "Tên nhân viên bắt buộc.");

                if (!model.RoleId.HasValue || model.RoleId.Value <= 0)
                    ModelState.AddModelError(nameof(model.RoleId), "Vui lòng chọn vai trò.");
                else if (!await _roleRepository.ExistsAsync(model.RoleId.Value))
                    ModelState.AddModelError(nameof(model.RoleId), "Role không hợp lệ.");

                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Vui lòng điền đầy đủ thông tin!";
                    ViewBag.EditStaff = model;
                    ViewBag.Query = q;
                    return await ReloadManagePage(q, model);
                }

                var existing = await _accountService.GetByIdAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                // Không cho đổi email
                model.Email = existing.Email;

                // Map các field cho phép sửa
                existing.AccountName = model.AccountName;
                existing.PhoneNumber = model.PhoneNumber;
                existing.Status = model.Status;
                existing.IsDeleted = model.IsDeleted;
                existing.RoleId = model.RoleId; // cho phép đổi Role
                existing.UpdatedAt = DateTime.Now;

                // Ảnh
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var (ok, pathOrErr) = await SaveAvatarAsync(AvatarFile);
                    if (!ok)
                    {
                        ViewBag.ShowErrorModal = true;
                        ViewBag.ErrorMessage = pathOrErr;
                        ViewBag.EditStaff = model;
                        ViewBag.Query = q;
                        return await ReloadManagePage(q, model);
                    }
                    existing.Image = pathOrErr;
                }
                else
                {
                    existing.Image = RemoveImage ? null : ExistingImage;
                }

                var success = await _accountService.UpdateAsync(model.Id, existing);
                TempData["Success"] = success ? "Cập nhật nhân viên thành công!" : "Cập nhật thất bại!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                ViewBag.EditStaff = model;
                ViewBag.Query = q;
                return await ReloadManagePage(q, model);
            }
        }

        // ===================== TOGGLE STATUS =====================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, string? q)
        {
            try
            {
                var staff = await _accountService.GetByIdAsync(id);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                staff.IsDeleted = !staff.IsDeleted;
                staff.Status = staff.IsDeleted ? "Inactive" : "Active";
                staff.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, staff);
                TempData["Success"] = success
                    ? (staff.IsDeleted ? "Đã vô hiệu hoá nhân viên!" : "Đã kích hoạt lại nhân viên!")
                    : "Cập nhật trạng thái thất bại!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Manage), new { q });
            }
        }

        // ===================== HELPERS =====================
        private async Task<IActionResult> ReloadManagePage(string? q, AccountDto editModel)
        {
            await LoadRolesAsync();

            var allStaff = await _accountService.GetAllStaffAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLowerInvariant();
                allStaff = allStaff.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            ViewBag.EditStaff = editModel;
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountStaff.cshtml",
                allStaff.OrderByDescending(c => c.CreatedAt).ToList());
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            // Truyền RoleId/RoleName tối giản để Razor duyệt dynamic gọn hơn
            ViewBag.Roles = roles
                .OrderBy(r => r.RoleName)
                .Select(r => new { r.RoleId, r.RoleName })
                .ToList();
        }

        private async Task<(bool ok, string messageOrPath)> SaveAvatarAsync(IFormFile file)
        {
            var okTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/png","image/jpeg","image/jpg","image/webp","image/gif"
            };
            const long MAX = 2 * 1024 * 1024;

            if (!okTypes.Contains(file.ContentType))
                return (false, "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ PNG/JPG/WebP/GIF.");

            if (file.Length > MAX)
                return (false, "Kích thước ảnh tối đa 2MB.");

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "staff");
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var publicPath = $"/uploads/staff/{fileName}";
            return (true, publicPath);
        }
    }
}
