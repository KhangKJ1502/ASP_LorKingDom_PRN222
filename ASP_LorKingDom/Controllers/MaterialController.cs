using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndWarehouseOnly] // Warehouse: Material Management
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
        public async Task<IActionResult> SaveMaterial(int id, string name, string? description, bool isDeleted = false)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(name, description, isDeleted);
                else
                    await _service.UpdateAsync(id, name, description, isDeleted);

                TempData["Success"] = "Lưu chất liệu thành công!";
                return RedirectToAction(nameof(Manage));
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                if (id != 0)
                {
                    var dto = await _service.GetByIdAsync(id)
                              ?? new BLL.DTOs.MaterialDto { Id = id, Name = name, Description = description, IsDeleted = isDeleted };
                    ViewBag.EditMaterial = dto;
                }

                return View("~/Views/Admin/ManageMaterial.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
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
