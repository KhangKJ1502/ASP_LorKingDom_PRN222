using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndWarehouseOnly] // Warehouse: Price Range Management
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
        public async Task<IActionResult> SavePriceRange(int id, decimal priceRangeMin, decimal priceRangeMax, bool isDeleted = false)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(priceRangeMin, priceRangeMax, isDeleted);
                else
                    await _service.UpdateAsync(id, priceRangeMin, priceRangeMax, isDeleted);

                TempData["Success"] = "Lưu thành công!";
                return RedirectToAction(nameof(Manage));
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.Query = null;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                if (id != 0)
                {
                    var dto = await _service.GetByIdAsync(id)
                              ?? new BLL.DTOs.PriceRangeDto
                              {
                                  Id = id,
                                  PriceRangeMin = priceRangeMin,
                                  PriceRangeMax = priceRangeMax,
                                  IsDeleted = isDeleted
                              };
                    ViewBag.EditPriceRange = dto;
                }

                return View("~/Views/Admin/ManagePriceRange.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync();
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
