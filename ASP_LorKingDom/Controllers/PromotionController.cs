using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        public IActionResult Index() => RedirectToAction(nameof(Manage));

        // GET: /Promotion/Manage?keyword=...&page=1&pageSize=10
        public async Task<IActionResult> Manage(string? keyword, int page = 1, int pageSize = 10)
        {
            var paged = await _promotionService.SearchPagedAsync(keyword, page, pageSize);

            ViewBag.Keyword = keyword ?? "";

            // nếu vừa bấm Edit -> mở modal
            if (TempData["EditPromotionId"] is int pid && pid > 0)
            {
                var edit = await _promotionService.GetByIdAsync(pid);
                ViewBag.EditPromotion = edit;
                ViewBag.ShowErrorModal = true; // tái dùng flag để auto-open modal
            }

            return View("~/Views/Admin/ManagePromotion.cshtml", paged);
        }

        // GET: /Promotion/Edit/5?keyword=...&page=...&pageSize=...
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var data = await _promotionService.GetByIdAsync(id);
            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy khuyến mãi cần sửa.";
                return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
            }

            TempData["EditPromotionId"] = id;
            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        // POST: /Promotion/SavePromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePromotion(PromotionSaveDto dto, string? keyword, int page = 1, int pageSize = 10)
        {
            try
            {
                if (dto.PromotionId > 0)
                {
                    var ok = await _promotionService.UpdateAsync(new PromotionUpdateDto
                    {
                        PromotionId = dto.PromotionId,
                        PromotionCode = dto.PromotionCode,
                        Description = dto.Description,
                        DiscountPercent = dto.DiscountPercent,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = dto.Status
                    });
                    if (!ok) throw new InvalidOperationException("Cập nhật thất bại hoặc không tìm thấy bản ghi.");

                    TempData["Success"] = "✅ Cập nhật khuyến mãi thành công.";
                }
                else
                {
                    var created = await _promotionService.CreateAsync(new PromotionCreateDto
                    {
                        PromotionCode = dto.PromotionCode,
                        Description = dto.Description,
                        DiscountPercent = dto.DiscountPercent,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
                    });

                    TempData["Success"] = "✅ Thêm khuyến mãi mới thành công.";
                }

                return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
            }
            catch (Exception ex)
            {
                // lỗi form -> load lại list + mở modal với dữ liệu người dùng nhập
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = ex.Message;

                var paged = await _promotionService.SearchPagedAsync(keyword, page, pageSize);

                ViewBag.Keyword = keyword ?? "";

                ViewBag.EditPromotion = new PromotionDto
                {
                    PromotionId = dto.PromotionId,
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                return View("~/Views/Admin/ManagePromotion.cshtml", paged);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var ok = await _promotionService.SoftDeleteAsync(id);
            TempData["toast"] = ok ? "🗑️ Đã xóa mềm." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var ok = await _promotionService.RestoreAsync(id);
            TempData["toast"] = ok ? "♻️ Đã khôi phục." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var ok = await _promotionService.ToggleStatusAsync(id);
            TempData["toast"] = ok ? "🔁 Đã đổi trạng thái." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status, string? keyword, int page = 1, int pageSize = 10)
        {
            var ok = await _promotionService.SetStatusAsync(id, status);
            TempData["toast"] = ok ? $"⚙️ Đặt trạng thái: {status}." : "⚠️ Không tìm thấy.";
            return RedirectToAction(nameof(Manage), new { keyword, page, pageSize });
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

        [HttpGet]
        public async Task<IActionResult> CheckCodeExists(string code, int? excludeId)
        {
            var exists = await _promotionService.ExistsByNameAsync(code, excludeId);
            return Json(new { valid = !exists });
        }

        [HttpGet]
        public async Task<IActionResult> CheckOverlap(DateTime startDate, DateTime endDate, int? excludeId)
        {
            var overlap = await _promotionService.HasOverlapAsync(0, startDate, endDate, excludeId);
            return Json(new { valid = !overlap });
        }
    }
}
