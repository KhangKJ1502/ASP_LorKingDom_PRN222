using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class BlogCategoryController : Controller
    {
        private readonly IBlogCategoryService _service;

        public BlogCategoryController(IBlogCategoryService service)
        {
            _service = service;
        }

        // GET: /BlogCategory/Manage
        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync();
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageBlogCategory.cshtml", list);
        }

        // POST: /BlogCategory/SaveBlogCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBlogCategory(int id, string name, string? description, bool isDeleted = false)
        {
            try
            {
                if (id == 0)
                {
                    // Tạo mới
                    var dto = new BlogCategoryDto
                    {
                        BlogCategoryName = name,
                        Description = description,
                        CreatedAt = DateTime.Now
                    };
                    await _service.CreateAsync(dto);
                }
                else
                {
                    // Cập nhật
                    var existing = await _service.GetByIdAsync(id);
                    if (existing == null)
                        throw new InvalidOperationException("Không tìm thấy chuyên mục blog.");

                    existing.BlogCategoryName = name;
                    existing.Description = description;
                    await _service.UpdateAsync(id, existing);
                }

                TempData["Success"] = "Lưu chuyên mục blog thành công!";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                if (id != 0)
                {
                    var dto = await _service.GetByIdAsync(id)
                              ?? new BlogCategoryDto { BlogCategoryId = id, BlogCategoryName = name, Description = description };
                    ViewBag.EditBlogCategory = dto;
                }

                return View("~/Views/Admin/ManageBlogCategory.cshtml", list);
            }
        }

        // GET: /BlogCategory/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy chuyên mục blog.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditBlogCategory = dto;
            return View("~/Views/Admin/ManageBlogCategory.cshtml", list);
        }

        // POST: /BlogCategory/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.SoftDeleteAsync(id);
                if (!success)
                    throw new InvalidOperationException("Không thể xóa chuyên mục blog.");

                TempData["Success"] = "Xóa chuyên mục blog thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}
