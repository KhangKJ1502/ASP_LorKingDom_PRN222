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

        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync(q);
            ViewBag.Query = q;
            ViewBag.SuperCategories = await _superService.GetActiveAsync(); // dropdown
            return View("~/Views/Admin/ManageCategory.cshtml", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(int id, int superCategoryId, string name, bool isDeleted = false)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(superCategoryId, name, isDeleted);
                else
                    await _service.UpdateAsync(id, superCategoryId, name, isDeleted);

                TempData["Success"] = "Lưu danh mục thành công!";
                return RedirectToAction(nameof(Manage));
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                ViewBag.SuperCategories = await _superService.GetActiveAsync();

                if (id != 0)
                {
                    var dto = await _service.GetByIdAsync(id)
                              ?? new BLL.DTOs.CategoryDto { Id = id, SuperCategoryId = superCategoryId, Name = name, IsDeleted = isDeleted };
                    ViewBag.EditCategory = dto;
                }

                return View("~/Views/Admin/ManageCategory.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                ViewBag.SuperCategories = await _superService.GetActiveAsync();
                return View("~/Views/Admin/ManageCategory.cshtml", list);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditCategory = dto;
            ViewBag.SuperCategories = await _superService.GetActiveAsync();
            return View("~/Views/Admin/ManageCategory.cshtml", list);
        }
    }
}
