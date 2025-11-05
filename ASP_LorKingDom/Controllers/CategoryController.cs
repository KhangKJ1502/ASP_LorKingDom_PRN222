using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    //[Authorize(AuthenticationSchemes = "AdminScheme")]
    //[AdminAndWarehouseOnly] // Warehouse: Category Management
    public class CategoryController : Controller
    {
        private readonly ICategoryService _service;
        private readonly ISuperCategoryService _superService;

        public CategoryController(ICategoryService service, ISuperCategoryService superService)
        {
            _service = service;
            _superService = superService;
        }

        private async Task<IActionResult> ReturnManageViewAsync(
            string? q, int page, int pageSize,
            object? editDto = null,
            string? error = null, bool showError = false)
        {
            var paged = await _service.GetPagedAsync(q, page, pageSize);

            ViewBag.Query = q;
            ViewBag.Page = paged.Page;
            ViewBag.PageSize = paged.PageSize;
            ViewBag.Total = paged.Total;
            ViewBag.SuperCategories = await _superService.GetActiveAsync();

            if (editDto != null) ViewBag.EditCategory = editDto;
            if (showError && !string.IsNullOrWhiteSpace(error))
            {
                ViewBag.ErrorMessage = error;
                ViewBag.ShowErrorModal = true;
            }

            return View("~/Views/Admin/ManageCategory.cshtml", paged);
        }

        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 8)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 8;
            return await ReturnManageViewAsync(q, page, pageSize);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(
    int superCategoryId, string name, bool isDeleted = false,
    string? q = null, int page = 1, int pageSize = 8)
        {
            try
            {
                await _service.CreateAsync(superCategoryId, name, isDeleted);
                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }
            catch (ArgumentException ex)
            {
                return await ReturnManageViewAsync(q, page, pageSize,
                    error: ex.Message, showError: true);
            }
            catch (InvalidOperationException ex)
            {
                return await ReturnManageViewAsync(q, page, pageSize,
                    error: ex.Message, showError: true);
            }
            catch (Exception ex)
            {
                return await ReturnManageViewAsync(q, page, pageSize,
                    error: "Đã có lỗi xảy ra. " + ex.Message, showError: true);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(
            int id, int superCategoryId, string name, bool isDeleted,
            string? q = null, int page = 1, int pageSize = 8)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, superCategoryId, name, isDeleted);
                if (!ok)
                    TempData["Error"] = "Không tìm thấy danh mục.";
                else
                    TempData["Success"] = "Cập nhật danh mục thành công!";

                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }
            catch (ArgumentException ex)
            {
                var edit = await _service.GetByIdAsync(id) ?? new BLL.DTOs.CategoryDto
                {
                    Id = id,
                    SuperCategoryId = superCategoryId,
                    Name = name,
                    IsDeleted = isDeleted
                };
                return await ReturnManageViewAsync(q, page, pageSize,
                    editDto: edit, error: ex.Message, showError: true);
            }
            catch (InvalidOperationException ex)
            {
                return await ReturnManageViewAsync(q, page, pageSize,
                    error: ex.Message, showError: true);
            }
            catch (Exception ex)
            {
                return await ReturnManageViewAsync(q, page, pageSize,
                    error: "Đã có lỗi xảy ra. " + ex.Message, showError: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? q = null, int page = 1, int pageSize = 8)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }

            return await ReturnManageViewAsync(q, page, pageSize, editDto: dto);
        }
    }
}
