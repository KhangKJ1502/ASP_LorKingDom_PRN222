using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly]
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;
        private readonly IProductService _productService;

        public PromotionController(IPromotionService promotionService, IProductService productService)
        {
            _promotionService = promotionService;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> ManageProducts(int promotionId, string? q, int page = 1, int pageSize = 20)
        {
            var promo = await _promotionService.GetByIdAsync(promotionId);
            if (promo == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
            var paged = await _productService.GetAdminPagedAsync(q, page, pageSize);
            foreach (var p in paged.Items)
            {
                if (p.PromotionId == promotionId)
                    p.IsOnSale = true; 
            }
            ViewBag.Promotion = promo;
            ViewBag.Query = q ?? "";
            return View("~/Views/Admin/ManagePromotionProducts.cshtml", paged);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignProducts(int promotionId, int[]? productIds)
        {
            var promotion = await _promotionService.GetByIdAsync(promotionId);
            if (promotion == null)
            {
                TempData["ErrorMessage"] = "Promotion không tồn tại.";
                return RedirectToAction(nameof(Index));
            }
            if (promotion.Status != "Active")
            {
                TempData["ErrorMessage"] = "Chỉ có thể gán sản phẩm cho promotion đang Active.";
                return RedirectToAction(nameof(ManageProducts), new { promotionId });
            }
            var now = DateTime.Now;
            if (now < promotion.StartDate)
            {
                TempData["ErrorMessage"] = $"Promotion chưa bắt đầu (ngày bắt đầu: {promotion.StartDate:dd/MM/yyyy}).";
                return RedirectToAction(nameof(ManageProducts), new { promotionId });
            }

            if (now > promotion.EndDate)
            {
                TempData["ErrorMessage"] = $"Promotion đã hết hạn (ngày kết thúc: {promotion.EndDate:dd/MM/yyyy}).";
                return RedirectToAction(nameof(ManageProducts), new { promotionId });
            }

            // If productIds is null or empty, treat as remove all products from this promotion
            var all = await _productService.GetAllAsync(null);
            var currentlyAssigned = all.Where(p => p.PromotionId == promotionId).Select(p => p.Id).ToHashSet();

            if (productIds == null || productIds.Length == 0)
            {
                foreach (var id in currentlyAssigned)
                    await _productService.SetPromotionAsync(id, null);
            }
            else
            {
                var selected = productIds.Distinct().ToHashSet();
                foreach (var pid in selected)
                    await _productService.SetPromotionAsync(pid, promotionId);
                var toUnassign = currentlyAssigned.Except(selected);
                foreach (var id in toUnassign)
                    await _productService.SetPromotionAsync(id, null);
            }

            TempData["SuccessMessage"] = "Cập nhật sản phẩm cho khuyến mãi thành công.";
            return RedirectToAction(nameof(ManageProducts), new { promotionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int promotionId, int productId)
        {
            var ok = await _productService.SetPromotionAsync(productId, null);
            if (ok) TempData["SuccessMessage"] = "Đã gỡ khuyến mãi khỏi sản phẩm.";
            else TempData["ErrorMessage"] = "Không thể gỡ khuyến mãi.";
            return RedirectToAction(nameof(ManageProducts), new { promotionId });
        }

        // ===== INDEX - View List (Không filter) =====
        [HttpGet("Promotion/Manage")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            await PrepareManagePageAsync(null, page, pageSize);
            return View("~/Views/Admin/ManagePromotion.cshtml",
                ViewData["PagedResult"] as PagedResult<PromotionDto>);
        }

        // ===== SEARCH - Tìm kiếm với keyword =====
        [HttpGet]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            await PrepareManagePageAsync(keyword, page, pageSize);
            return View("~/Views/Admin/ManagePromotion.cshtml",
                ViewData["PagedResult"] as PagedResult<PromotionDto>);
        }

        // ===========================
        // GET: /Promotion/Edit/{id}
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var data = await _promotionService.GetByIdAsync(id);
            if (data == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi cần sửa.";
                return RedirectToListOrSearch(keyword, page, pageSize);
            }
            TempData["EditPromotionId"] = id;

            return RedirectToListOrSearch(keyword, page, pageSize);
        }

        // ===========================
        // POST: /Promotion/CreatePromotion
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(
            PromotionSaveDto dto,
            string? keyword,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                await _promotionService.CreateAsync(new PromotionCreateDto
                {
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
                });
                TempData["SuccessMessage"] = "Thêm khuyến mãi mới thành công!";
                return RedirectToListOrSearch(keyword, page, pageSize);
            }
            catch (ArgumentException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.AddPromotion = new PromotionDto
                {
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                };
                await PrepareManagePageAsync(keyword, page, pageSize);
                return View("~/Views/Admin/ManagePromotion.cshtml", 
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.AddPromotion = new PromotionDto
                {
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                };
                
                await PrepareManagePageAsync(keyword, page, pageSize);
                return View("~/Views/Admin/ManagePromotion.cshtml", 
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi không xác định: {ex.Message}";
                ViewBag.AddPromotion = new PromotionDto
                {
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                };
                
                await PrepareManagePageAsync(keyword, page, pageSize);
                return View("~/Views/Admin/ManagePromotion.cshtml", 
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
        }

        // ===========================
        // POST: /Promotion/UpdatePromotion
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePromotion(
            PromotionSaveDto dto,
            string? keyword,
            int page = 1,
            int pageSize = 10)
        {
            if (dto.PromotionId <= 0)
            {
                TempData["ErrorMessage"] = "ID khuyến mãi không hợp lệ.";
                return RedirectToListOrSearch(keyword, page, pageSize);
            }
            try
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

                if (!ok)
                {
                    TempData["ErrorMessage"] = "Cập nhật thất bại hoặc không tìm thấy bản ghi.";
                    return RedirectToListOrSearch(keyword, page, pageSize);
                }

                TempData["SuccessMessage"] = "Cập nhật khuyến mãi thành công!";
                return RedirectToListOrSearch(keyword, page, pageSize);
            }
            catch (ArgumentException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.EditPromotion = new PromotionDto
                {
                    PromotionId = dto.PromotionId,
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                };
                
                await PrepareManagePageAsync(keyword, page, pageSize);
                return View("~/Views/Admin/ManagePromotion.cshtml", 
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.EditPromotion = new PromotionDto
                {
                    PromotionId = dto.PromotionId,
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                };
                
                await PrepareManagePageAsync(keyword, page, pageSize);
                return View("~/Views/Admin/ManagePromotion.cshtml", 
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi không xác định: {ex.Message}";
                ViewBag.EditPromotion = new PromotionDto
                {
                    PromotionId = dto.PromotionId,
                    PromotionCode = dto.PromotionCode,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = dto.Status
                };
                
                await PrepareManagePageAsync(keyword, page, pageSize);
                return View("~/Views/Admin/ManagePromotion.cshtml", 
                    ViewData["PagedResult"] as PagedResult<PromotionDto>);
            }
        }


        // ===========================
        // POST: /Promotion/Delete
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await _promotionService.DeleteAsync(id);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result 
                ? "Xóa khuyến mãi thành công!" 
                : "Không tìm thấy khuyến mãi cần xóa.";

            return RedirectToListOrSearch(keyword, page, pageSize);
        }

        // ===========================
        // POST: /Promotion/ToggleStatus
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await _promotionService.ToggleStatusAsync(id);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result 
                ? "Đổi trạng thái khuyến mãi thành công!" 
                : "Không tìm thấy khuyến mãi cần đổi trạng thái.";

            return RedirectToListOrSearch(keyword, page, pageSize);
        }

        // ===========================
        // POST: /Promotion/SetStatus
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status, string? keyword, int page = 1, int pageSize = 10)
        {
            var result = await _promotionService.SetStatusAsync(id, status);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result 
                ? $"Đặt trạng thái khuyến mãi thành '{status}' thành công!" 
                : "Không tìm thấy khuyến mãi cần đặt trạng thái.";

            return RedirectToListOrSearch(keyword, page, pageSize);
        }

        // ===========================
        // GET: /Promotion/Active
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var list = await _promotionService.GetActiveAsync();
            return View("~/Views/Admin/PromotionActive.cshtml", list);
        }

        // ===========================
        // GET: /Promotion/ActiveByProduct/{productId}
        // ===========================
        [HttpGet]
        public async Task<IActionResult> ActiveByProduct(int productId)
        {
            var list = await _promotionService.GetActiveByProductAsync(productId);
            ViewBag.ProductId = productId;
            return View("~/Views/Admin/PromotionActive.cshtml", list);
        }

        // ===========================
        // GET: /Promotion/CheckCodeExists?code=...&excludeId=...
        // ===========================
        [HttpGet]
        public async Task<IActionResult> CheckCodeExists(string code, int? excludeId)
        {
            var exists = await _promotionService.ExistsByNameAsync(code, excludeId);
            return Json(new { valid = !exists });
        }

        // ===========================
        // GET: /Promotion/CheckOverlap?startDate=...&endDate=...&excludeId=...
        // ===========================
        [HttpGet]
        public async Task<IActionResult> CheckOverlap(DateTime startDate, DateTime endDate, int? excludeId)
        {
            var overlap = await _promotionService.HasOverlapAsync(0, startDate, endDate, excludeId);
            return Json(new { valid = !overlap });
        }


        /// <summary>
        /// Load danh sách phân trang, set ViewBag và (nếu có) edit dto để modal dùng.
        /// Kết quả phân trang sẽ đặt trong ViewData["PagedResult"] để action có thể return view.
        /// </summary>
        private async Task PrepareManagePageAsync(string? keyword, int page, int pageSize)
        {
            var paged = await _promotionService.SearchPagedAsync(keyword, page, pageSize);
            ViewBag.Keyword = keyword ?? "";

            // Chỉ set EditPromotion từ TempData nếu ViewBag chưa có (không có lỗi validation)
            if (ViewBag.EditPromotion == null && ViewBag.AddPromotion == null)
            {
                if (TempData["EditPromotionId"] is int pid && pid > 0)
                {
                    var edit = await _promotionService.GetByIdAsync(pid);
                    if (edit != null)
                    {
                        ViewBag.EditPromotion = edit;
                    }
                }
            }

            ViewData["PagedResult"] = paged;
        }

        // Helper method to avoid repeated redirect logic
        private IActionResult RedirectToListOrSearch(string? keyword, int page, int pageSize)
        {
            return string.IsNullOrWhiteSpace(keyword)
                ? RedirectToAction(nameof(Index), new { page, pageSize })
                : RedirectToAction(nameof(Search), new { keyword, page, pageSize });
        }
    }
}
