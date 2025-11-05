using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    //[Authorize(AuthenticationSchemes = "AdminScheme")]
    //[AdminAndWarehouseOnly] // Warehouse: Price Range Management
    public class PriceRangeController : Controller
    {
        private readonly IPriceRangeService _service;

        public PriceRangeController(IPriceRangeService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync(q);
            ViewBag.Query = q;
            return View("~/Views/Admin/ManagePriceRange.cshtml", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPriceRange(decimal priceRangeMin, decimal priceRangeMax, bool isDeleted = false, string? q = null)
        {
            try
            {
                await _service.CreateAsync(priceRangeMin, priceRangeMax, isDeleted);
                TempData["Success"] = "Thêm khoảng giá thành công!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (ArgumentException ex) 
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra: " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriceRange(int id, decimal priceRangeMin, decimal priceRangeMax, bool isDeleted, string? q = null)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, priceRangeMin, priceRangeMax, isDeleted);
                if (!ok)
                    TempData["Error"] = "Không tìm thấy khoảng giá.";
                else
                    TempData["Success"] = "Cập nhật khoảng giá thành công!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (ArgumentException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;

                var dto = await _service.GetByIdAsync(id) ?? new BLL.DTOs.PriceRangeDto
                {
                    Id = id,
                    PriceRangeMin = priceRangeMin,
                    PriceRangeMax = priceRangeMax,
                    IsDeleted = isDeleted
                };
                ViewBag.EditPriceRange = dto;

                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.EditPriceRange = await _service.GetByIdAsync(id);
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.EditPriceRange = await _service.GetByIdAsync(id);
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra: " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy khoảng giá.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditPriceRange = dto;
            return View("~/Views/Admin/ManagePriceRange.cshtml", list);
        }
    }
}
