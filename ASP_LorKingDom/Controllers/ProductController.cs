using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BLL.DTOs;
using BLL.Interfaces;
using BLL.IServices;

namespace WebUI.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productSvc;
        private readonly ICategoryService _categorySvc;
        private readonly ISexService _sexSvc;
        private readonly IPriceRangeService _priceRangeSvc;
        private readonly IBrandService _brandSvc;
        private readonly IAgeService _ageSvc;
        private readonly IMaterialService _materialSvc;
        private readonly IOriginService _originSvc;

        public ProductController(
            IProductService productSvc,
            ICategoryService categorySvc,
            ISexService sexSvc,
            IPriceRangeService priceRangeSvc,
            IBrandService brandSvc,
            IAgeService ageSvc,
            IMaterialService materialSvc,
            IOriginService originSvc)
        {
            _productSvc = productSvc;
            _categorySvc = categorySvc;
            _sexSvc = sexSvc;
            _priceRangeSvc = priceRangeSvc;
            _brandSvc = brandSvc;
            _ageSvc = ageSvc;
            _materialSvc = materialSvc;
            _originSvc = originSvc;
        }

        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _productSvc.GetAllAsync(q);
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageProduct.cshtml", list);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View("~/Views/Admin/AddProduct.cshtml", new ProductDto());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _productSvc.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction(nameof(Manage));
            }

            await LoadDropdownsAsync();
            return View("~/Views/Admin/AddProduct.cshtml", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
     ProductDto dto,
     IFormFile? MainImageUpload,
     IFormFile[]? DetailImages,
     [FromServices] IWebHostEnvironment env)
        {
            const string SUB = "assets/Component/img_product";

            dto.SecondaryImageUrls ??= new System.Collections.Generic.List<string>();

            if (MainImageUpload is { Length: > 0 })
                dto.MainImageUrl = await SaveFileAsync(MainImageUpload, env, SUB);

            if (DetailImages is { Length: > 0 })
            {
                foreach (var f in DetailImages.Take(8))
                {
                    if (f is { Length: > 0 })
                    {
                        var url = await SaveFileAsync(f, env, SUB);
                        dto.SecondaryImageUrls.Add(url);
                    }
                }
            }

            // ⛳️ Áp ràng buộc khi tạo mới
            if (dto.Id == 0)
            {
                if (dto.CategoryId is null) ModelState.AddModelError(nameof(dto.CategoryId), "Vui lòng chọn Danh mục.");
                if (dto.BrandId is null) ModelState.AddModelError(nameof(dto.BrandId), "Vui lòng chọn Thương hiệu.");
                if (dto.SexId is null) ModelState.AddModelError(nameof(dto.SexId), "Vui lòng chọn Giới tính.");
                if (dto.AgeId is null) ModelState.AddModelError(nameof(dto.AgeId), "Vui lòng chọn Khoảng tuổi.");
                if (dto.MaterialId is null) ModelState.AddModelError(nameof(dto.MaterialId), "Vui lòng chọn Chất liệu.");
                if (dto.OriginId is null) ModelState.AddModelError(nameof(dto.OriginId), "Vui lòng chọn Nguồn gốc.");
                if (dto.PriceRangeId is null) ModelState.AddModelError(nameof(dto.PriceRangeId), "Vui lòng chọn Khoảng giá.");
                if (string.IsNullOrWhiteSpace(dto.MainImageUrl))
                    ModelState.AddModelError("MainImageUpload", "Vui lòng chọn Ảnh chính.");
            }

            // ✅ NEW: nếu có lỗi ModelState => hiển thị Toast y hệt Category
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();

                // Gộp thông điệp ngắn gọn cho Toast (chỉ 1–2 dòng)
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                 ?? "Dữ liệu không hợp lệ.";

                ViewBag.ErrorMessage = firstError;
                ViewBag.ShowErrorModal = true;   // ← cờ bật Toast
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }

            try
            {
                if (dto.Id == 0)
                {
                    await _productSvc.CreateAsync(dto);
                    TempData["Success"] = "Thêm sản phẩm thành công!";
                }
                else
                {
                    var ok = await _productSvc.UpdateAsync(dto);
                    if (!ok)
                    {
                        await LoadDropdownsAsync();
                        ViewBag.ErrorMessage = "Không tìm thấy sản phẩm để cập nhật.";
                        ViewBag.ShowErrorModal = true;   // ← bật Toast
                        return View("~/Views/Admin/AddProduct.cshtml", dto);
                    }
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                }

                return RedirectToAction(nameof(Manage));
            }
            catch (DbUpdateException dbex)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = "DB error: " + (dbex.InnerException?.Message ?? dbex.Message);
                ViewBag.ShowErrorModal = true;       // ← bật Toast
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }
            catch (Exception ex)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = ex.GetBaseException().Message;
                ViewBag.ShowErrorModal = true;       // ← bật Toast
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Categories = await _categorySvc.GetActiveAsync();
            ViewBag.Brands = await _brandSvc.GetActiveAsync();
            ViewBag.Materials = await _materialSvc.GetActiveAsync();
            ViewBag.Ages = await _ageSvc.GetActiveAsync();
            ViewBag.Sexes = await _sexSvc.GetActiveAsync();
            ViewBag.Origins = await _originSvc.GetActiveAsync();
            ViewBag.PriceRanges = await _priceRangeSvc.GetActiveAsync();
        }

        private static async Task<string> SaveFileAsync(IFormFile file, IWebHostEnvironment env, string subFolder)
        {
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ (chỉ .jpg, .jpeg, .png, .webp).");

            var root = Path.Combine(env.WebRootPath, subFolder);
            Directory.CreateDirectory(root);

            var name = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(root, name);

            using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            return "/" + Path.Combine(subFolder, name).Replace("\\", "/");
        }
    }
}
