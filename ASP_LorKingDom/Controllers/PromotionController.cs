// WebUI/Controllers/PromotionController.cs
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;
        private readonly IProductService? _productService;

        public PromotionController(IPromotionService promotionService, IProductService? productService = null)
        {
            _promotionService = promotionService;
            _productService = productService;
        }

        public IActionResult Index() => RedirectToAction(nameof(Manage));

        // GET: /Promotion/Manage?keyword=...
        public async Task<IActionResult> Manage(string? keyword)
        {
            var promotions = await _promotionService.GetAllAsync(keyword);
            ViewBag.Keyword = keyword ?? "";

            if (_productService != null)
            {
                var items = await _productService.GetSelectListAsync();
                ViewBag.Products = items
                    .Select(p => new SelectListItem { Value = p.Value, Text = p.Text })
                    .ToList();
            }

            if (TempData["EditPromotionId"] is int pid && pid > 0)
            {
                var edit = await _promotionService.GetByIdAsync(pid);
                ViewBag.EditPromotion = edit;
            }

            return View("~/Views/Admin/ManagePromotion.cshtml", promotions);
        }

        // GET: /Promotion/Edit/5?keyword=...
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? keyword)
        {
            var data = await _promotionService.GetByIdAsync(id);
            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy khuyến mãi cần sửa.";
                return RedirectToAction(nameof(Manage), new { keyword });
            }
            TempData["EditPromotionId"] = id;
            return RedirectToAction(nameof(Manage), new { keyword });
        }

        // POST: /Promotion/SavePromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePromotion(PromotionSaveDto dto, string? keyword)
        {
            try
            {
                if (dto.PromotionId > 0)
                {
                    var ok = await _promotionService.UpdateAsync(new PromotionUpdateDto
                    {
                        PromotionId = dto.PromotionId,
                        ProductId = dto.ProductId,
                        Name = dto.Name,
                        Description = dto.Description,
                        DiscountPercent = dto.DiscountPercent,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = dto.Status,
                        IsDeleted = dto.IsDeleted
                    });
                    if (!ok) throw new InvalidOperationException("Cập nhật thất bại hoặc không tìm thấy bản ghi.");
                    TempData["Success"] = "✅ Cập nhật khuyến mãi thành công.";
                }
                else
                {
                    var created = await _promotionService.CreateAsync(new PromotionCreateDto
                    {
                        ProductId = dto.ProductId,
                        Name = dto.Name,
                        Description = dto.Description,
                        DiscountPercent = dto.DiscountPercent,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
                    });
                    TempData["Success"] = "✅ Thêm khuyến mãi mới thành công.";
                }

                return RedirectToAction(nameof(Manage), new { keyword });
            }
            catch (Exception ex)
            {
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = ex.Message;

                var promotions = await _promotionService.GetAllAsync(keyword);
                if (_productService != null)
                {
                    var items = await _productService.GetSelectListAsync();
                    ViewBag.Products = items.Select(p => new SelectListItem { Value = p.Value, Text = p.Text }).ToList();
                }
                ViewBag.Keyword = keyword ?? "";

                ViewBag.EditPromotion = dto.PromotionId > 0
                    ? new PromotionDto
                    {
                        PromotionId = dto.PromotionId,
                        ProductId = dto.ProductId,
                        Name = dto.Name,
                        Description = dto.Description,
                        DiscountPercent = dto.DiscountPercent,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = dto.Status,
                        IsDeleted = dto.IsDeleted
                    }
                    : null;

                return View("~/Views/Admin/ManagePromotion.cshtml", promotions);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var ok = await _promotionService.SoftDeleteAsync(id);
            TempData["toast"] = ok ? "🗑️ Đã xóa mềm." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var ok = await _promotionService.RestoreAsync(id);
            TempData["toast"] = ok ? "♻️ Đã khôi phục." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var ok = await _promotionService.ToggleStatusAsync(id);
            TempData["toast"] = ok ? "🔁 Đã đổi trạng thái." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var ok = await _promotionService.SetStatusAsync(id, status);
            TempData["toast"] = ok ? $"⚙️ Đặt trạng thái: {status}." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var list = await _promotionService.GetActiveAsync();
            return View("~/Views/Admin/PromotionActive.cshtml", list);
        }

        [HttpGet]
        public async Task<IActionResult> ActiveByProduct(int productId)
        {
            var list = await _promotionService.GetActiveByProductAsync(productId);
            ViewBag.ProductId = productId;
            return View("~/Views/Admin/PromotionActive.cshtml", list);
        }

        // AJAX validations
        [HttpGet]
        public async Task<IActionResult> CheckNameExists(string name, int? excludeId)
        {
            var exists = await _promotionService.ExistsByNameAsync(name, excludeId);
            return Json(new { valid = !exists });
        }

        [HttpGet]
        public async Task<IActionResult> CheckOverlap(int productId, DateTime startDate, DateTime endDate, int? excludeId)
        {
            var overlap = await _promotionService.HasOverlapAsync(productId, startDate, endDate, excludeId);
            return Json(new { valid = !overlap });
        }
    }
}
