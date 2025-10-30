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

        [HttpGet]
        public async Task<IActionResult> Manage(string? q)
        {
            var all = await _accountService.GetAllCustomerForAdminAsync(); // hiển thị hết

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                all = all.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountCustomer.cshtml", all.OrderByDescending(c => c.CreatedAt).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? q)
        {
            var customer = await _accountService.GetByIdAsync(id);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng!";
                return RedirectToAction(nameof(Manage), new { q });
            }

            ViewBag.Query = q;
            ViewBag.EditCustomer = customer;

            var all = await _accountService.GetAllCustomerForAdminAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                all = all.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            return View("~/Views/Admin/ManageAccountCustomer.cshtml", all.OrderByDescending(c => c.CreatedAt).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> SaveCustomer(AccountDto model, string? q)
        {
            try
            {
                if (model.Id == 0)
                {
                    TempData["Error"] = "Không thể thêm khách hàng từ dashboard!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                if (string.IsNullOrWhiteSpace(model.AccountName))
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Tên khách hàng bắt buộc.";
                    ViewBag.EditCustomer = model;
                    ViewBag.Query = q;
                    return await ReloadManagePage(q, model);
                }

                // unique phone
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                    await _accountService.ExistsByPhoneNumberAsync(model.PhoneNumber, model.Id))
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Số điện thoại đã tồn tại.";
                    ViewBag.EditCustomer = model;
                    return await ReloadManagePage(q, model);
                }

                var existing = await _accountService.GetByIdAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                existing.AccountName = model.AccountName;
                existing.PhoneNumber = model.PhoneNumber;
                existing.Status = model.IsDeleted ? "Inactive" : model.Status;
                existing.IsDeleted = model.IsDeleted;
                existing.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(model.Id, existing);
                TempData["Success"] = success ? "Cập nhật thành công!" : "Cập nhật thất bại!";
                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                ViewBag.ShowErrorModal = true;
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                ViewBag.EditCustomer = model;
                ViewBag.Query = q;
                return await ReloadManagePage(q, model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockAccount(int id, string? q)
        {
            try
            {
                var c = await _accountService.GetByIdAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                if (c.IsDeleted)
                {
                    TempData["Error"] = "Khách hàng đã bị chặn rồi!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                c.IsDeleted = true;
                c.Status = "Inactive";
                c.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, c);
                TempData["Success"] = success
                    ? "🚫 Đã chặn khách hàng thành công!"
                    : "⚠️ Chặn khách hàng thất bại!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Manage), new { q });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnblockAccount(int id, string? q)
        {
            try
            {
                var c = await _accountService.GetByIdAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                if (!c.IsDeleted)
                {
                    TempData["Error"] = "Khách hàng chưa bị chặn!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                c.IsDeleted = false;
                c.Status = "Active";
                c.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, c);
                TempData["Success"] = success
                    ? "✅ Đã bỏ chặn khách hàng thành công!"
                    : "⚠️ Bỏ chặn khách hàng thất bại!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Manage), new { q });
            }
        }

        /// <summary>
        /// [DEPRECATED] Sử dụng BlockAccount() hoặc UnblockAccount() thay thế
        /// </summary>
        [HttpPost]
        [Obsolete("Use BlockAccount() or UnblockAccount() instead")]
        public async Task<IActionResult> ToggleStatus(int id, string? q)
        {
            try
            {
                var c = await _accountService.GetByIdAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                c.IsDeleted = !c.IsDeleted;
                c.Status = c.IsDeleted ? "Inactive" : "Active";
                c.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, c);
                TempData["Success"] = success
                    ? (c.IsDeleted ? "Đã vô hiệu hoá khách hàng!" : "Đã kích hoạt lại khách hàng!")
                    : "Cập nhật trạng thái thất bại!";

                return RedirectToAction(nameof(Manage), new { q });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Manage), new { q });
            }
        }

        private async Task<IActionResult> ReloadManagePage(string? q, AccountDto editModel)
        {
            var all = await _accountService.GetAllCustomerForAdminAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                all = all.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            ViewBag.EditCustomer = editModel;
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountCustomer.cshtml", all.OrderByDescending(c => c.CreatedAt).ToList());
        }
    }
}
