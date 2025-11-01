using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountCustomerController : Controller
    {
        private readonly IAccountService _accountService;
        public AccountCustomerController(IAccountService accountService) => _accountService = accountService;

        // ===== INDEX - View List (Không filter) =====
        [HttpGet]
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

        // ===== MANAGE - Xử lý cả list và search =====
        [HttpGet]
        public async Task<IActionResult> Manage(string? q, int page = 1, int pageSize = 10)
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
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { page, pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, page, pageSize });
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
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                if (string.IsNullOrWhiteSpace(model.AccountName))
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Tên khách hàng bắt buộc.";
                    ViewBag.EditCustomer = model;
                    ViewBag.Query = q;
                    return await ReloadManagePage(q, 1, pageSize, model);
                }

                // unique phone
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(model.PhoneNumber, model.Id))
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Số điện thoại đã tồn tại.";
                    ViewBag.EditCustomer = model;
                    return await ReloadManagePage(q, 1, pageSize, model);
                }

                var existing = await _accountService.GetByIdAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                existing.AccountName = model.AccountName;
                existing.PhoneNumber = model.PhoneNumber;
                existing.Status = model.IsDeleted ? "Inactive" : model.Status;
                existing.IsDeleted = model.IsDeleted;
                existing.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(model.Id, existing);
                TempData["Success"] = success ? "Cập nhật thành công!" : "Cập nhật thất bại!";
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
            catch (Exception ex)
            {
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                ViewBag.EditCustomer = model;
                ViewBag.Query = q;
                return await ReloadManagePage(q, 1, pageSize, model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockAccount(int id, string? q, int pageSize = 10)
        {
            try
            {
                var c = await _accountService.GetByIdAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                if (c.IsDeleted)
                {
                    TempData["Error"] = "Khách hàng đã bị chặn rồi!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                c.IsDeleted = true;
                c.Status = "Inactive";
                c.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, c);
                TempData["Success"] = success
                    ? "🚫 Đã chặn khách hàng thành công!"
                    : "⚠️ Chặn khách hàng thất bại!";

                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnblockAccount(int id, string? q, int pageSize = 10)
        {
            try
            {
                var c = await _accountService.GetByIdAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                if (!c.IsDeleted)
                {
                    TempData["Error"] = "Khách hàng chưa bị chặn!";
                    return string.IsNullOrWhiteSpace(q) 
                        ? RedirectToAction(nameof(Index), new { pageSize }) 
                        : RedirectToAction(nameof(Search), new { q, pageSize });
                }

                c.IsDeleted = false;
                c.Status = "Active";
                c.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, c);
                TempData["Success"] = success
                    ? "✅ Đã bỏ chặn khách hàng thành công!"
                    : "⚠️ Bỏ chặn khách hàng thất bại!";

                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return string.IsNullOrWhiteSpace(q) 
                    ? RedirectToAction(nameof(Index), new { pageSize }) 
                    : RedirectToAction(nameof(Search), new { q, pageSize });
            }
        }

        // ===== Helpers =====
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
