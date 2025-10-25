using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly] // Chỉ Admin và Staff mới quản lý được blog category
    public class BlogCategoryController : Controller
    {
        private readonly IBlogCategoryService _service;

        public BlogCategoryController(IBlogCategoryService service)
        {
            _service = service;
        }

        // GET: /BlogCategory/Manage
        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 10)
        {
            var allCategories = await _service.GetAllAsync();

            // Tìm kiếm theo tên hoặc mô tả
            if (!string.IsNullOrWhiteSpace(q))
            {
                allCategories = allCategories
                    .Where(c =>
                        (c.BlogCategoryName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sắp xếp theo ngày cập nhật gần nhất
            allCategories = allCategories.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt).ToList();

            // Pagination
            var totalCount = allCategories.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedCategories = allCategories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View("~/Views/Admin/ManageBlogCategory.cshtml", pagedCategories);
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
                    return Ok(new { success = true, message = "Chuyên mục blog đã được tạo thành công!" });
                }
                else
                {
                    // Cập nhật
                    var existing = await _service.GetByIdAsync(id);
                    if (existing == null)
                        return BadRequest(new { success = false, message = "Không tìm thấy chuyên mục blog." });

                    existing.BlogCategoryName = name;
                    existing.Description = description;
                    await _service.UpdateAsync(id, existing);
                    return Ok(new { success = true, message = "Chuyên mục blog đã được cập nhật thành công!" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        // GET: /BlogCategory/GetById/{id}
        [HttpGet("BlogCategory/GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var category = await _service.GetByIdAsync(id);
                if (category == null)
                    return NotFound();

                return Json(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: /BlogCategory/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
                return RedirectToAction(nameof(Manage));

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
                    return BadRequest(new { success = false, message = "Không thể xóa chuyên mục blog." });

                return Ok(new { success = true, message = "Chuyên mục blog đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
    }
}
