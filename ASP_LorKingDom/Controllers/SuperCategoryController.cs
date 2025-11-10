using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 8)
            => await ReturnManageViewAsync(q, page, pageSize);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSuperCategory(
    string name, bool isDeleted = false,
    string? q = null, int page = 1, int pageSize = 8)
        {
            try
            {
                await _service.CreateAsync(name, isDeleted);
                TempData["Success"] = "Thêm danh mục tổng thành công!";
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }
            catch (ArgumentException ex)
            {
                ViewBag.LastName = name;
                return await ReturnManageViewAsync(q, page, pageSize, error: ex.Message, showError: true);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.LastName = name;
                return await ReturnManageViewAsync(q, page, pageSize, error: ex.Message, showError: true);
            }
            catch (Exception ex)
            {
                ViewBag.LastName = name;
                return await ReturnManageViewAsync(q, page, pageSize, error: "Đã có lỗi xảy ra. " + ex.Message, showError: true);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSuperCategory(
            int id, string name, bool isDeleted,
            string? q = null, int page = 1, int pageSize = 8)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, name, isDeleted);
                if (!ok)
                    TempData["Error"] = "Không tìm thấy danh mục tổng.";
                else
                    TempData["Success"] = "Cập nhật danh mục tổng thành công!";

                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }
            catch (ArgumentException ex)
            {
                var edit = await _service.GetByIdAsync(id) ?? new BLL.DTOs.SuperCategoryDto
                {
                    Id = id,
                    Name = name,
                    IsDeleted = isDeleted
                };
                return await ReturnManageViewAsync(q, page, pageSize,
                    editDto: edit, error: ex.Message, showError: true);
            }
            catch (InvalidOperationException ex)
            {
                var edit = await _service.GetByIdAsync(id);
                return await ReturnManageViewAsync(q, page, pageSize,
                    editDto: edit, error: ex.Message, showError: true);
            }
            catch (Exception ex)
            {
                var edit = await _service.GetByIdAsync(id);
                return await ReturnManageViewAsync(q, page, pageSize,
                    editDto: edit, error: "Đã có lỗi xảy ra. " + ex.Message, showError: true);
            }
        }

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
