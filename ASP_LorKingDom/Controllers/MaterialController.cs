using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    //[Authorize(AuthenticationSchemes = "AdminScheme")]
    //[AdminAndWarehouseOnly] // Warehouse: Material Management
    public class MaterialController : Controller
    {
        private readonly IMaterialService _service;

        public MaterialController(IMaterialService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync(q);
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageMaterial.cshtml", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterial(string name, string? description, bool isDeleted = false, string? q = null)
        {
            try
            {
                await _service.CreateAsync(name, description, isDeleted);
                TempData["Success"] = "Thêm chất liệu thành công!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (ArgumentException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaterial(int id, string name, string? description, bool isDeleted, string? q = null)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, name, description, isDeleted);
                if (!ok)
                    TempData["Error"] = "Không tìm thấy chất liệu.";
                else
                    TempData["Success"] = "Cập nhật chất liệu thành công!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (ArgumentException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                var dto = await _service.GetByIdAsync(id) ?? new BLL.DTOs.MaterialDto
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    IsDeleted = isDeleted
                };
                ViewBag.EditMaterial = dto;

                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                ViewBag.EditMaterial = await _service.GetByIdAsync(id);
                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                ViewBag.EditMaterial = await _service.GetByIdAsync(id);
                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy chất liệu.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditMaterial = dto;
            return View("~/Views/Admin/ManageMaterial.cshtml", list);
        }
    }
}
