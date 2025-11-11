using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly]
    public class BlogCategoryController : Controller
    {
        private readonly IBlogCategoryService _service;

        public BlogCategoryController(IBlogCategoryService service)
        {
            _service = service;
        }


        /// Display blog category management page with search and pagination
        /// GET: /BlogCategory/Manage
        /// <param name="q">Search query for category name or description</param>
        /// <param name="page">Current page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 10)
        {
            // Get all active categories
            var allCategories = await _service.GetAllAsync();

            // Apply search filter if query exists
            if (!string.IsNullOrWhiteSpace(q))
            {
                allCategories = allCategories
                    .Where(c =>
                        (c.BlogCategoryName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort by most recently updated
            allCategories = allCategories
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ToList();

            // Calculate pagination
            var totalCount = allCategories.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Get items for current page
            var pagedCategories = allCategories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass data to view
            ViewBag.Query = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View("~/Views/Admin/ManageBlogCategory.cshtml", pagedCategories);
        }

        /// Get blog category details by ID 
        /// GET: /BlogCategory/GetById/{id}
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var category = await _service.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { success = false, message = "Category not found." });
                }

                return Json(category);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// Display edit form for a specific blog category
        /// GET: /BlogCategory/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            // Get category to edit
            var category = await _service.GetByIdAsync(id);
            if (category == null)
            {
                return RedirectToAction(nameof(Manage));
            }

            // Get all categories for the list view
            var allCategories = await _service.GetAllAsync();

            // Pass edit category to view
            ViewBag.EditBlogCategory = category;

            return View("~/Views/Admin/ManageBlogCategory.cshtml", allCategories);
        }


        /// Create or update a blog category
        /// POST: /BlogCategory/SaveBlogCategory
        /// <param name="id">Category ID (0 for create, >0 for update)</param>
        /// <param name="name">Category name</param>
        /// <param name="description">Category description (optional)</param>
        /// <param name="isDeleted">Soft delete flag (not used in create/update)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveBlogCategory(int id, string name, string? description, bool isDeleted = false)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Category name is required." });
                }

                if (id == 0)
                {
                    // Create new category
                    var newCategory = new BlogCategoryDto
                    {
                        BlogCategoryName = name.Trim(),
                        Description = description?.Trim(),
                        CreatedAt = DateTime.Now
                    };

                    await _service.CreateAsync(newCategory);
                    return Json(new { success = true, message = "Blog category created successfully!" });
                }
                else
                {
                    // Update existing category
                    var existingCategory = await _service.GetByIdAsync(id);
                    if (existingCategory == null)
                    {
                        return Json(new { success = false, message = "Category not found." });
                    }

                    existingCategory.BlogCategoryName = name.Trim();
                    existingCategory.Description = description?.Trim();

                    await _service.UpdateAsync(id, existingCategory);
                    return Json(new { success = true, message = "Blog category updated successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// Soft delete a blog category
        /// POST: /BlogCategory/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                var success = await _service.SoftDeleteAsync(id);
                if (!success)
                {
                    return Json(new { success = false, message = "Failed to delete blog category." });
                }

                return Json(new { success = true, message = "Blog category deleted successfully!" });
            }
            catch (InvalidOperationException ex)
            {
                // Handle case when category has related blog posts
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
