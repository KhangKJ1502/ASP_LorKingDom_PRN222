using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    //[Authorize(AuthenticationSchemes = "AdminScheme")]
    //[AdminAndWarehouseOnly] // Warehouse: Brand Management
    public class BrandController : Controller
    {
        private readonly IBrandService _service;

        public BrandController(IBrandService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync(q);
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageBrand.cshtml", list);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SaveBrand(int id, string name, bool isDeleted = false)
        //{
        //    try
        //    {
        //        if (id == 0)
        //            await _service.CreateAsync(name, isDeleted);
        //        else
        //            await _service.UpdateAsync(id, name, isDeleted);

        //        TempData["Success"] = "Lưu thương hiệu thành công!";
        //        return RedirectToAction(nameof(Manage));
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        var list = await _service.GetAllAsync();
        //        ViewBag.ErrorMessage = ex.Message;
        //        ViewBag.ShowErrorModal = true;

        //        if (id != 0)
        //        {
        //            var dto = await _service.GetByIdAsync(id)
        //                      ?? new BLL.DTOs.BrandDto { Id = id, Name = name, IsDeleted = isDeleted };
        //            ViewBag.EditBrand = dto;
        //        }

        //        return View("~/Views/Admin/ManageBrand.cshtml", list);
        //    }
        //    catch (Exception ex)
        //    {
        //        var list = await _service.GetAllAsync();
        //        ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
        //        ViewBag.ShowErrorModal = true;
        //        return View("~/Views/Admin/ManageBrand.cshtml", list);
        //    }
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBrand(string name, bool isDeleted = false, string? q = null)
        {
            try
            {
                await _service.CreateAsync(name, isDeleted);
                TempData["Success"] = "Thêm thương hiệu thành công!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true; // dùng toast lỗi
                return View("~/Views/Admin/ManageBrand.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageBrand.cshtml", list);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBrand(int id, string name, bool isDeleted, string? q = null)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, name, isDeleted);
                if (!ok)
                {
                    TempData["Error"] = "Không tìm thấy thương hiệu.";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                TempData["Success"] = "Cập nhật thương hiệu thành công!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (InvalidOperationException ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ShowErrorModal = true;

                // Giữ lại dữ liệu đang sửa để mở lại modal
                var dto = await _service.GetByIdAsync(id)
                          ?? new BLL.DTOs.BrandDto { Id = id, Name = name, IsDeleted = isDeleted };
                ViewBag.EditBrand = dto;

                return View("~/Views/Admin/ManageBrand.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync(q);
                ViewBag.Query = q;
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;

                var dto = await _service.GetByIdAsync(id)
                          ?? new BLL.DTOs.BrandDto { Id = id, Name = name, IsDeleted = isDeleted };
                ViewBag.EditBrand = dto;

                return View("~/Views/Admin/ManageBrand.cshtml", list);
            }
        }
    
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy thương hiệu.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditBrand = dto;
            return View("~/Views/Admin/ManageBrand.cshtml", list);
        }
    }
}
