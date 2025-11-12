using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Filters;


namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndWarehouseOnly] // Warehouse: Product Management
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
        private readonly IStatisticsService _statisticsService;
        private readonly IPromotionService _promotionSvc;


        private readonly IProductImageService _imageSvc;
        public ProductController(
          IProductService productSvc,
          IProductImageService imageSvc,
          ICategoryService categorySvc,
          ISexService sexSvc,
          IPriceRangeService priceRangeSvc,
          IBrandService brandSvc,
          IAgeService ageSvc,
          IMaterialService materialSvc,
          IOriginService originSvc,
          IStatisticsService statisticsService,
           IPromotionService promotionSvc)

        {
            _productSvc = productSvc;
            _imageSvc = imageSvc;
            _categorySvc = categorySvc;
            _sexSvc = sexSvc;
            _priceRangeSvc = priceRangeSvc;
            _brandSvc = brandSvc;
            _ageSvc = ageSvc;
            _materialSvc = materialSvc;
            _originSvc = originSvc;
            _statisticsService = statisticsService;
            _promotionSvc = promotionSvc;
        }

        [HttpGet]
        public async Task<IActionResult> AssignPromotion(int id)
        {
            var dto = await _productSvc.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction(nameof(Manage));
            }

            var promos = await _promotionSvc.GetActiveAsync();
            ViewBag.Promotions = promos;
            await LoadDropdownsAsync();
            return View("~/Views/Admin/AssignPromotion.cshtml", dto);
        }


        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 8)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 8;

            var paged = await _productSvc.GetAdminPagedAsync(q, page, pageSize);
            ViewBag.Query = q;
            ViewBag.Page = paged.Page;
            ViewBag.PageSize = paged.PageSize;
            ViewBag.Total = paged.Total;

            return View("~/Views/Admin/ManageProduct.cshtml", paged);
        }


        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View("~/Views/Admin/AddProduct.cshtml", new ProductDto());
        }
        // Get 
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
        public async Task<IActionResult> Add(
       ProductDto dto,
       IFormFile? MainImageUpload,
       IFormFile[]? DetailImages,
       string[]? ExistingImageUrls,
       bool? KeepMainImage,
       [FromServices] IWebHostEnvironment env)
        {
            const string SUB = "assets/Component/img_product";
            dto.SecondaryImageUrls ??= new List<string>();

            if (MainImageUpload is { Length: > 0 })
                dto.MainImageUrl = await SaveFileAsync(MainImageUpload, env, SUB);

            if (DetailImages is { Length: > 0 })
            {
                foreach (var f in DetailImages.Take(6))
                {
                    if (f is { Length: > 0 })
                    {
                        var url = await SaveFileAsync(f, env, SUB);
                        dto.SecondaryImageUrls.Add(url);
                    }
                }
            }

            if (dto.CategoryId is null) ModelState.AddModelError(nameof(dto.CategoryId), "Vui lòng chọn Danh mục.");
            if (dto.BrandId is null) ModelState.AddModelError(nameof(dto.BrandId), "Vui lòng chọn Thương hiệu.");
            if (dto.SexId is null) ModelState.AddModelError(nameof(dto.SexId), "Vui lòng chọn Giới tính.");
            if (dto.AgeId is null) ModelState.AddModelError(nameof(dto.AgeId), "Vui lòng chọn Khoảng tuổi.");
            if (dto.MaterialId is null) ModelState.AddModelError(nameof(dto.MaterialId), "Vui lòng chọn Chất liệu.");
            if (dto.OriginId is null) ModelState.AddModelError(nameof(dto.OriginId), "Vui lòng chọn Nguồn gốc.");
            if (dto.PriceRangeId is null) ModelState.AddModelError(nameof(dto.PriceRangeId), "Vui lòng chọn Khoảng giá.");
            if (string.IsNullOrWhiteSpace(dto.MainImageUrl))
                ModelState.AddModelError("MainImageUpload", "Vui lòng chọn Ảnh chính.");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }

            try
            {
                var newId = await _productSvc.CreateAsync(dto);
                dto.Id = newId;
                TempData["Success"] = "Thêm sản phẩm thành công!";

                await _imageSvc.UpsertImagesAsync(
                    productId: dto.Id,
                    mainImageUrl: dto.MainImageUrl,
                    keepSecondaryUrls: ExistingImageUrls ?? Array.Empty<string>(),
                    addSecondaryUrls: dto.SecondaryImageUrls ?? new List<string>(),
                    keepMainIfNull: true
                );

                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = ex.GetBaseException().Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            ProductDto dto,
            IFormFile? MainImageUpload,
            IFormFile[]? DetailImages,
            string[]? ExistingImageUrls,
            bool? KeepMainImage,
            [FromServices] IWebHostEnvironment env)
        {
            const string SUB = "assets/Component/img_product";
            dto.SecondaryImageUrls ??= new List<string>();

            if (MainImageUpload is { Length: > 0 })
                dto.MainImageUrl = await SaveFileAsync(MainImageUpload, env, SUB);

            if (DetailImages is { Length: > 0 })
            {
                foreach (var f in DetailImages.Take(6))
                {
                    if (f is { Length: > 0 })
                    {
                        var url = await SaveFileAsync(f, env, SUB);
                        dto.SecondaryImageUrls.Add(url);
                    }
                }
            }

            if (dto.Id == 0)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = "Thiếu Id sản phẩm để cập nhật.";
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/AddProduct.cshtml", dto);
            }

            try
            {
                var ok = await _productSvc.UpdateAsync(dto);
                if (!ok)
                {
                    await LoadDropdownsAsync();
                    ViewBag.ErrorMessage = "Không tìm thấy sản phẩm để cập nhật.";
                    ViewBag.ShowErrorModal = true;
                    return View("~/Views/Admin/AddProduct.cshtml", dto);
                }
                TempData["Success"] = "Cập nhật sản phẩm thành công!";

                await _imageSvc.UpsertImagesAsync(
                    productId: dto.Id,
                    mainImageUrl: dto.MainImageUrl,
                    keepSecondaryUrls: ExistingImageUrls ?? Array.Empty<string>(),
                    addSecondaryUrls: dto.SecondaryImageUrls ?? new List<string>(),
                    keepMainIfNull: KeepMainImage != false
                );

                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                await LoadDropdownsAsync();
                ViewBag.ErrorMessage = ex.GetBaseException().Message;
                ViewBag.ShowErrorModal = true;
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