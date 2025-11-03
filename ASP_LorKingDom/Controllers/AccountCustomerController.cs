using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly] // Staff: Customer Management
    [AutoValidateAntiforgeryToken]
    public class AccountCustomerController : Controller
    {
        private readonly IAccountService _accountService;
        public AccountCustomerController(IAccountService accountService) => _accountService = accountService;

        // ===== INDEX - View List (Không filter) =====
        [HttpGet("AccountCustomer/Manage")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var allItems = await _accountService.GetAllCustomerForAdminAsync();
            var orderedItems = allItems.OrderByDescending(c => c.CreatedAt).ToList();

            var pagedResult = GetPagedResult(orderedItems, page, pageSize);
            ViewBag.Query = null;

            return View("~/Views/Admin/ManageAccountCustomer.cshtml", pagedResult);
        }

        // ===== SEARCH - Tìm kiếm với query =====
        [HttpGet]
        public async Task<IActionResult> Search(string? q, int page = 1, int pageSize = 10)
        {
            var allItems = await _accountService.GetAllCustomerForAdminAsync();
            var filteredItems = Filter(allItems, q);
            var orderedItems = filteredItems.OrderByDescending(c => c.CreatedAt).ToList();

            var pagedResult = GetPagedResult(orderedItems, page, pageSize);
            ViewBag.Query = q;

            return View("~/Views/Admin/ManageAccountCustomer.cshtml", pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? q, int page = 1, int pageSize = 10)
        {
            var customer = await _accountService.GetByIdAsync(id);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng!";
                return RedirectToListOrSearch(q, page, pageSize);
            }

            ViewBag.Query = q;
            ViewBag.EditCustomer = customer;

            var all = await _accountService.GetAllCustomerForAdminAsync();
            var filtered = Filter(all, q);
            var ordered = filtered.OrderByDescending(c => c.CreatedAt).ToList();

            var pagedResult = GetPagedResult(ordered, page, pageSize);
            return View("~/Views/Admin/ManageAccountCustomer.cshtml", pagedResult);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCustomer(AccountDto model, string? q, int pageSize = 10)
        {
            try
            {
                if (model.Id == 0)
                {
                    TempData["Error"] = "Không thể thêm khách hàng từ dashboard!";
                    return RedirectToListOrSearch(q, 1, pageSize);
                }

                if (string.IsNullOrWhiteSpace(model.AccountName))
                {
                    return await ShowValidationError("Tên khách hàng bắt buộc.", model, q, pageSize);
                }

                var existing = await _accountService.GetByIdAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToListOrSearch(q, 1, pageSize);
                }

                // Update model
                existing.AccountName = model.AccountName.Trim();
                existing.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
                existing.Status = model.IsDeleted ? "Inactive" : model.Status;
                existing.IsDeleted = model.IsDeleted;
                existing.UpdatedAt = DateTime.Now;

                // Service sẽ validate phone format và unique
                var success = await _accountService.UpdateAsync(model.Id, existing);
                TempData["Success"] = success ? "✅ Cập nhật thành công!" : "❌ Cập nhật thất bại!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            catch (InvalidOperationException ex)
            {
                // Validation errors từ Service
                return await ShowValidationError(ex.Message, model, q, pageSize);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToListOrSearch(q, 1, pageSize);
            }
            catch (Exception ex)
            {
                return await ShowValidationError($"Lỗi: {ex.Message}", model, q, pageSize);
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockAccount(int id, string? q, int pageSize = 10)
        {
            var c = await _accountService.GetByIdAsync(id);
            if (c == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }

            if (c.IsDeleted)
            {
                TempData["Error"] = "Khách hàng đã bị chặn rồi!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }

            c.IsDeleted = true;
            c.Status = "Inactive";
            c.UpdatedAt = DateTime.Now;

            var success = await _accountService.UpdateAsync(id, c);
            TempData["Success"] = success
                ? "🚫 Đã chặn khách hàng thành công!"
                : "⚠️ Chặn khách hàng thất bại!";

            return RedirectToListOrSearch(q, 1, pageSize);
        }

        [HttpPost]
        public async Task<IActionResult> UnblockAccount(int id, string? q, int pageSize = 10)
        {
            var c = await _accountService.GetByIdAsync(id);
            if (c == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }

            if (!c.IsDeleted)
            {
                TempData["Error"] = "Khách hàng chưa bị chặn!";
                return RedirectToListOrSearch(q, 1, pageSize);
            }

            c.IsDeleted = false;
            c.Status = "Active";
            c.UpdatedAt = DateTime.Now;

            var success = await _accountService.UpdateAsync(id, c);
            TempData["Success"] = success
                ? "✅ Đã bỏ chặn khách hàng thành công!"
                : "⚠️ Bỏ chặn khách hàng thất bại!";

            return RedirectToListOrSearch(q, 1, pageSize);
        }

        // ===== Helpers =====
        private IActionResult RedirectToListOrSearch(string? q, int page, int pageSize)
        {
            return string.IsNullOrWhiteSpace(q)
                ? RedirectToAction(nameof(Index), new { page, pageSize })
                : RedirectToAction(nameof(Search), new { q, page, pageSize });
        }

        private async Task<IActionResult> ShowValidationError(string errorMessage, AccountDto model, string? q, int pageSize)
        {
            ViewBag.ShowErrorModal = true;
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.EditCustomer = model;
            ViewBag.Query = q;
            return await ReloadManagePage(q, 1, pageSize, model);
        }

        private static List<AccountDto> Filter(List<AccountDto> all, string? q)
        {
            if (string.IsNullOrWhiteSpace(q)) return all;
            var kw = q.Trim().ToLowerInvariant();
            return all.Where(c =>
                (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                (c.Email?.ToLower().Contains(kw) ?? false) ||
                (c.PhoneNumber?.Contains(kw) ?? false)).ToList();
        }

        private PagedResult<AccountDto> GetPagedResult(List<AccountDto> items, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var totalItems = items.Count;
            var pagedItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<AccountDto>
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                Total = totalItems
            };
        }

        private async Task<IActionResult> ReloadManagePage(string? q, int page, int pageSize, AccountDto? editModel = null)
        {
            var all = await _accountService.GetAllCustomerForAdminAsync();
            var filtered = Filter(all, q);
            var ordered = filtered.OrderByDescending(c => c.CreatedAt).ToList();

            var pagedResult = GetPagedResult(ordered, page, pageSize);

            if (editModel != null) ViewBag.EditCustomer = editModel;
            ViewBag.Query = q;

            return View("~/Views/Admin/ManageAccountCustomer.cshtml", pagedResult);
        }
    }
}
