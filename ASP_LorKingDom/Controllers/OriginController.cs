using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    //[Authorize(AuthenticationSchemes = "AdminScheme")]
    //[AdminAndWarehouseOnly] // Warehouse: Origin Management
    public class OriginController : Controller
    {
        private readonly IOriginService _service;

        public OriginController(IOriginService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync(q);
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageOrigin.cshtml", list);
        }

        // ===== Add =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrigin(string name, bool isDeleted = false, string? q = null)
        {
            try
            {
                await _service.CreateAsync(name, isDeleted);
                TempData["Success"] = "Thêm xuất xứ thành công!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (ArgumentException ex) // nếu có validator ném lỗi
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
        }

        // ===== Update =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrigin(int id, string name, bool isDeleted, string? q = null)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, name, isDeleted);
                if (!ok)
                    TempData["Error"] = "Không tìm thấy xuất xứ.";
                else
                    TempData["Success"] = "Cập nhật xuất xứ thành công!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (ArgumentException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;

                // Giữ lại data đang sửa để mở lại modal
                var dto = await _service.GetByIdAsync(id)
                          ?? new BLL.DTOs.OriginDto { Id = id, Name = name, IsDeleted = isDeleted };
                ViewBag.EditOrigin = dto;

                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.EditOrigin = await _service.GetByIdAsync(id);
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.EditOrigin = await _service.GetByIdAsync(id);
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy xuất xứ.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditOrigin = dto;
            return View("~/Views/Admin/ManageOrigin.cshtml", list);
        }
    }
}
