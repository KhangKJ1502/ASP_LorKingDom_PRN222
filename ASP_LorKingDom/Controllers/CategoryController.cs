using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _service;
        private readonly ISuperCategoryService _superService;

        public CategoryController(ICategoryService service, ISuperCategoryService superService)
        {
            _service = service;
            _superService = superService;
        }

        // ========== Helpers ==========
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

        // ========== Manage ==========
        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 8)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 8;
            return await ReturnManageViewAsync(q, page, pageSize);
        }

        // ========== Save ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(
            int id, int superCategoryId, string name, bool isDeleted = false,
            string? q = null, int page = 1, int pageSize = 8)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(superCategoryId, name, isDeleted);
                else
                    await _service.UpdateAsync(id, superCategoryId, name, isDeleted);

                TempData["Success"] = "Lưu danh mục thành công!";
                // Giữ lại điều kiện hiện tại
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }
            catch (InvalidOperationException ex)
            {
                // Trả về đúng kiểu PagedResult + bật toast + giữ context
                object? edit = null;
                if (id != 0)
                {
                    edit = await _service.GetByIdAsync(id)
                           ?? new BLL.DTOs.CategoryDto
                           {
                               Id = id,
                               SuperCategoryId = superCategoryId,
                               Name = name,
                               IsDeleted = isDeleted
                           };
                }

                return await ReturnManageViewAsync(
                    q, page, pageSize,
                    editDto: edit,
                    error: ex.Message,
                    showError: true
                );
            }
            catch (Exception ex)
            {
                return await ReturnManageViewAsync(
                    q, page, pageSize,
                    error: "Đã có lỗi xảy ra. " + ex.Message,
                    showError: true
                );
            }
        }

        // ========== Edit (mở modal) ==========
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? q = null, int page = 1, int pageSize = 8)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(Manage), new { q, page, pageSize });
            }

            // Trả về Manage với EditCategory để mở modal
            return await ReturnManageViewAsync(q, page, pageSize, editDto: dto);
        }
    }
}
