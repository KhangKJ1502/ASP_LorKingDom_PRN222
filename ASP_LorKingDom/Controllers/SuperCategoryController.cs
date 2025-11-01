using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndWarehouseOnly] // Warehouse: Super Category Management
    public class SuperCategoryController : Controller
    {
        private readonly ISuperCategoryService _service;

        public SuperCategoryController(ISuperCategoryService service)
        {
            _service = service;
        }

        // ===== Helper: luôn trả về PagedResult và set ViewBag =====
        private async Task<IActionResult> ReturnManageViewAsync(
            string? q, int page, int pageSize,
            object? editDto = null,
            string? error = null, bool showError = false)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 8;

            var paged = await _service.GetPagedAsync(q, page, pageSize);

            ViewBag.Query = q;
            ViewBag.Page = paged.Page;
            ViewBag.PageSize = paged.PageSize;
            ViewBag.Total = paged.Total;

            if (editDto != null) ViewBag.EditSuperCategory = editDto;
            if (showError && !string.IsNullOrWhiteSpace(error))
            {
                ViewBag.ErrorMessage = error;
                ViewBag.ShowErrorModal = true;
            }

            return View("~/Views/Admin/ManageSuperCategory.cshtml", paged);
        }

        // ===== GET: /SuperCategory/Manage =====
        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 8)
            => await ReturnManageViewAsync(q, page, pageSize);

        // ===== POST: /SuperCategory/SaveSuperCategory =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSuperCategory(
            int id, string name, bool isDeleted = false,
            string? q = null, int page = 1, int pageSize = 8)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(name, isDeleted);
                else
                    await _service.UpdateAsync(id, name, isDeleted);

                TempData["Success"] = "Lưu thành công!";
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }
            catch (InvalidOperationException ex)
            {
                object? edit = null;

                // Hiển thị lại modal Edit nếu đang sửa
                if (id != 0)
                {
                    edit = await _service.GetByIdAsync(id)
                           ?? new BLL.DTOs.SuperCategoryDto
                           {
                               Id = id,
                               Name = name,
                               IsDeleted = isDeleted
                           };
                }
                // Nếu đang thêm mới, bạn có thể dùng ViewBag.LastName để fill lại input name ở View
                if (id == 0) ViewBag.LastName = name;

                return await ReturnManageViewAsync(q, page, pageSize,
                    editDto: edit,
                    error: ex.Message,
                    showError: true);
            }
            catch (Exception ex)
            {
                return await ReturnManageViewAsync(q, page, pageSize,
                    error: "Đã có lỗi xảy ra. " + ex.Message,
                    showError: true);
            }
        }

        // ===== GET: /SuperCategory/Edit/{id} (mở modal) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? q = null, int page = 1, int pageSize = 8)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục tổng.";
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }

            return await ReturnManageViewAsync(q, page, pageSize, editDto: dto);
        }
    }
}
