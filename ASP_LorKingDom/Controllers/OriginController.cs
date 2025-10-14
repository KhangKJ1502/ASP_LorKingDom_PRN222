using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveOrigin(int id, string name, bool isDeleted = false)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(name, isDeleted);
                else
                    await _service.UpdateAsync(id, name, isDeleted);

                TempData["Success"] = "Lưu xuất xứ thành công!";
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
                              ?? new BLL.DTOs.OriginDto { Id = id, Name = name, IsDeleted = isDeleted };
                    ViewBag.EditOrigin = dto;
                }

                return View("~/Views/Admin/ManageOrigin.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync();
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
