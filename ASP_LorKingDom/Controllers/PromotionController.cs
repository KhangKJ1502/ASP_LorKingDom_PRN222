using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly] // Staff: Promotion Management
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        // ===== INDEX - View List (Không filter) =====
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            await PrepareManagePageAsync(null, page, pageSize);
            return View("~/Views/Admin/ManagePromotion.cshtml",
                ViewData["PagedResult"] as PagedResult<PromotionDto>);
        }

        // ===== SEARCH - Tìm kiếm với keyword =====
        [HttpGet]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            await PrepareManagePageAsync(keyword, page, pageSize);
            return View("~/Views/Admin/ManagePromotion.cshtml",
                ViewData["PagedResult"] as PagedResult<PromotionDto>);
        }

        // ===== MANAGE - Xử lý cả list và search =====
        [HttpGet]
        public async Task<IActionResult> Manage(string? keyword, int page = 1, int pageSize = 10)
        {
            await PrepareManagePageAsync(keyword, page, pageSize);
            return View("~/Views/Admin/ManagePromotion.cshtml",
                ViewData["PagedResult"] as PagedResult<PromotionDto>);
        }

        // ===========================
        // GET: /Promotion/Edit/{id}
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var data = await _promotionService.GetByIdAsync(id);
            if (data == null)
            {
                TempData["ErrorMessage"] = "❌ Không tìm thấy khuyến mãi cần sửa.";
                return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
            }

            // ghi nhớ id để Manage() biết mở modal edit
            TempData["EditPromotionId"] = id;

            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        // ===========================
        // POST: /Promotion/CreatePromotion
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(
            PromotionSaveDto dto,
            string? keyword,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var created = await _promotionService.CreateAsync(new PromotionCreateDto
                {
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
                });

                TempData["SuccessMessage"] = "✅ Thêm khuyến mãi mới thành công!";
                return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
            }
            catch (Exception ex)
            {
                // Hiển thị lại trang với Add modal và hiển thị lỗi
                TempData["ErrorMessage"] = $"❌ {ex.Message}";

                await PrepareManagePageAsync(keyword, page, pageSize,
                    forceEditDto: BuildPromotionDtoFromForm(dto),
                    showErrorModal: true,
                    errorMessage: ex.Message);

                ViewBag.ShowAddModal = true; // Flag để mở Add Modal
                ViewBag.EditPromotion = null; // Clear edit data

                return View("~/Views/Admin/ManagePromotion.cshtml",
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
        }

        // ===========================
        // POST: /Promotion/UpdatePromotion
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePromotion(
            PromotionSaveDto dto,
            string? keyword,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                if (dto.PromotionId <= 0)
                    throw new InvalidOperationException("ID khuyến mãi không hợp lệ.");

                var ok = await _promotionService.UpdateAsync(new PromotionUpdateDto
                {
                    PromotionId = dto.PromotionId,
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                });

                if (!ok)
                    throw new InvalidOperationException("Cập nhật thất bại hoặc không tìm thấy bản ghi.");

                TempData["SuccessMessage"] = "✅ Cập nhật khuyến mãi thành công!";
                return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
            }
            catch (Exception ex)
            {
                // Hiển thị lại trang với Edit modal và hiển thị lỗi
                TempData["ErrorMessage"] = $"❌ {ex.Message}";

                await PrepareManagePageAsync(keyword, page, pageSize,
                    forceEditDto: BuildPromotionDtoFromForm(dto),
                    showErrorModal: true,
                    errorMessage: ex.Message);

                return View("~/Views/Admin/ManagePromotion.cshtml",
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
        }

        /// <summary>
        /// [DEPRECATED] Use CreatePromotion() or UpdatePromotion() instead
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete("Use CreatePromotion() or UpdatePromotion() instead")]
        public async Task<IActionResult> SavePromotion(
            PromotionSaveDto dto,
            string? keyword,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                await HandleSavePromotionAsync(dto);

                TempData["SuccessMessage"] = dto.PromotionId > 0
                    ? "✅ Cập nhật khuyến mãi thành công!"
                    : "✅ Thêm khuyến mãi mới thành công!";

                return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
            }
            catch (Exception ex)
            {
                // nếu lỗi -> hiển thị lại trang + modal + dữ liệu user nhập + hiển thị lỗi
                TempData["ErrorMessage"] = $"❌ {ex.Message}";

                await PrepareManagePageAsync(keyword, page, pageSize,
                    forceEditDto: BuildPromotionDtoFromForm(dto),
                    showErrorModal: true,
                    errorMessage: ex.Message);

                return View("~/Views/Admin/ManagePromotion.cshtml",
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
        }

        // ===========================
        // POST: /Promotion/SoftDelete
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await DoSoftDeleteAsync(id);
            if (result)
                TempData["SuccessMessage"] = "🗑️ Đã xóa mềm khuyến mãi thành công!";
            else
                TempData["ErrorMessage"] = "❌ Không tìm thấy khuyến mãi cần xóa.";

            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        // ===========================
        // POST: /Promotion/Restore
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await DoRestoreAsync(id);
            if (result)
                TempData["SuccessMessage"] = "♻️ Đã khôi phục khuyến mãi thành công!";
            else
                TempData["ErrorMessage"] = "❌ Không tìm thấy khuyến mãi cần khôi phục.";

            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        // ===========================
        // POST: /Promotion/ToggleStatus
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await DoToggleStatusAsync(id);
            if (result)
                TempData["SuccessMessage"] = "🔁 Đã đổi trạng thái khuyến mãi thành công!";
            else
                TempData["ErrorMessage"] = "❌ Không tìm thấy khuyến mãi cần đổi trạng thái.";

            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        // ===========================
        // POST: /Promotion/SetStatus
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await DoSetStatusAsync(id, status);
            if (result)
                TempData["SuccessMessage"] = $"⚙️ Đã đặt trạng thái khuyến mãi thành '{status}' thành công!";
            else
                TempData["ErrorMessage"] = "❌ Không tìm thấy khuyến mãi cần đặt trạng thái.";

            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        // ===========================
        // GET: /Promotion/Active
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var list = await _promotionService.GetActiveAsync();
            return View("~/Views/Admin/PromotionActive.cshtml", list);
        }

        // ===========================
        // GET: /Promotion/ActiveByProduct/{productId}
        // ===========================
        [HttpGet]
        public async Task<IActionResult> ActiveByProduct(int productId)
        {
            var list = await _promotionService.GetActiveByProductAsync(productId);
            ViewBag.ProductId = productId;
            return View("~/Views/Admin/PromotionActive.cshtml", list);
        }

        // ===========================
        // GET: /Promotion/CheckCodeExists?code=...&excludeId=...
        // AJAX validate duy nhất mã
        // ===========================
        [HttpGet]
        public async Task<IActionResult> CheckCodeExists(string code, int? excludeId)
        {
            var exists = await _promotionService.ExistsByNameAsync(code, excludeId);
            return Json(new { valid = !exists });
        }

        // ===========================
        // GET: /Promotion/CheckOverlap?startDate=...&endDate=...&excludeId=...
        // AJAX validate trùng thời gian
        // ===========================
        [HttpGet]
        public async Task<IActionResult> CheckOverlap(DateTime startDate, DateTime endDate, int? excludeId)
        {
            var overlap = await _promotionService.HasOverlapAsync(0, startDate, endDate, excludeId);
            return Json(new { valid = !overlap });
        }

        // ============================================================
        // =============== PRIVATE HELPERS (logic tách riêng) =========
        // ============================================================

        /// <summary>
        /// Load danh sách phân trang, set ViewBag và (nếu có) edit dto để modal dùng.
        /// Kết quả phân trang sẽ đặt trong ViewData["PagedResult"] để action có thể return view.
        /// </summary>
        private async Task PrepareManagePageAsync(
            string? keyword,
            int page,
            int pageSize,
            PromotionDto? forceEditDto = null,
            bool showErrorModal = false,
            string? errorMessage = null)
        {
            // lấy list phân trang
            var paged = await _promotionService.SearchPagedAsync(keyword, page, pageSize);

            // ViewBag dùng trong view
            ViewBag.Keyword = keyword ?? "";

            // Nếu vừa từ Edit quay về (TempData["EditPromotionId"])
            PromotionDto? editData = forceEditDto;
            bool shouldOpenModal = showErrorModal;
            bool hasError = showErrorModal && !string.IsNullOrEmpty(errorMessage);

            if (!shouldOpenModal) // nếu chưa bị ép mở modal do lỗi form
            {
                if (TempData["EditPromotionId"] is int pid && pid > 0)
                {
                    var edit = await _promotionService.GetByIdAsync(pid);
                    if (edit != null)
                    {
                        editData = edit;
                        shouldOpenModal = true;
                    }
                }
            }

            if (editData != null)
            {
                ViewBag.EditPromotion = editData;
            }

            // Chỉ set ShowErrorModal = true khi thực sự có lỗi
            if (shouldOpenModal)
            {
                ViewBag.ShowModal = true; // Để JavaScript biết cần mở modal
            }

            if (hasError)
            {
                ViewBag.ShowErrorModal = true; // Chỉ set khi có lỗi thực sự
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ViewBag.ErrorMessage = errorMessage;
            }

            // đặt model cho view thông qua ViewData để return View(...) gọn hơn
            ViewData["PagedResult"] = paged;
        }

        /// <summary>
        /// Xử lý lưu khuyến mãi (create / update).
        /// Ném exception nếu fail để action SavePromotion bắt.
        /// </summary>
        private async Task HandleSavePromotionAsync(PromotionSaveDto dto)
        {
            if (dto.PromotionId > 0)
            {
                // UPDATE
                var ok = await _promotionService.UpdateAsync(new PromotionUpdateDto
                {
                    PromotionId = dto.PromotionId,
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                });

                if (!ok)
                    throw new InvalidOperationException("Cập nhật thất bại hoặc không tìm thấy bản ghi.");
            }
            else
            {
                // CREATE
                var created = await _promotionService.CreateAsync(new PromotionCreateDto
                {
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
                });
                // created có thể dùng nếu muốn trả lại data mới tạo
            }
        }

        /// <summary>
        /// Map lại data user nhập (PromotionSaveDto) thành PromotionDto
        /// để fill vào modal trong trường hợp lỗi form.
        /// </summary>
        private static PromotionDto BuildPromotionDtoFromForm(PromotionSaveDto dto)
        {
            return new PromotionDto
            {
                PromotionId = dto.PromotionId,
                PromotionCode = dto.PromotionCode,
                Description = dto.Description,
                DiscountPercent = dto.DiscountPercent,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status ?? "Active",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Soft delete promotion và trả về true/false.
        /// </summary>
        private async Task<bool> DoSoftDeleteAsync(int id)
        {
            return await _promotionService.DeleteAsync(id);
        }

        /// <summary>
        /// Restore promotion và trả về true/false.
        /// </summary>
        private async Task<bool> DoRestoreAsync(int id)
        {
            return await _promotionService.RestoreAsync(id);
        }

        /// <summary>
        /// Toggle status (Active <-> Inactive)
        /// </summary>
        private async Task<bool> DoToggleStatusAsync(int id)
        {
            return await _promotionService.ToggleStatusAsync(id);
        }

        /// <summary>
        /// Set status cụ thể ("Active" / "Inactive")
        /// </summary>
        private async Task<bool> DoSetStatusAsync(int id, string status)
        {
            return await _promotionService.SetStatusAsync(id, status);
        }
    }
}
