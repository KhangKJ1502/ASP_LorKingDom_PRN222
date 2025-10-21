using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class AccountCustomerController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountCustomerController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<IActionResult> Manage(string? q)
        {
            var allCustomers = await _accountService.GetAllCustomerAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim().ToLower();
                allCustomers = allCustomers.Where(c =>
                    (c.AccountName?.ToLower().Contains(keyword) ?? false) ||
                    (c.Email?.ToLower().Contains(keyword) ?? false) ||
                    (c.PhoneNumber?.Contains(keyword) ?? false)
                ).ToList();
            }

            var customerDtos = allCustomers
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountCustomer.cshtml", customerDtos);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string? q)
        {
            var customer = await _accountService.GetByIdAsync(id);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng!";
                return RedirectToAction(nameof(Manage), new { q });
            }

            ViewBag.Query = q;
            return View(customer);
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

            var allCustomers = await _accountService.GetAllCustomerAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                allCustomers = allCustomers.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            return View("~/Views/Admin/ManageAccountCustomer.cshtml",
                allCustomers.OrderByDescending(c => c.CreatedAt).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCustomer(AccountDto model, string? q)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.ShowErrorModal = true;
                    ViewBag.ErrorMessage = "Vui lòng điền đầy đủ thông tin!";
                    ViewBag.EditCustomer = model;
                    ViewBag.Query = q;
                    return await ReloadManagePage(q, model);
                }

                if (model.Id == 0)
                {
                    TempData["Error"] = "Không thể thêm khách hàng từ dashboard!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                var existing = await _accountService.GetByIdAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                existing.AccountName = model.AccountName;
                existing.PhoneNumber = model.PhoneNumber;
                existing.Status = model.Status;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? q)
        {
            try
            {
                var customer = await _accountService.GetByIdAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Không tìm thấy khách hàng!";
                    return RedirectToAction(nameof(Manage), new { q });
                }

                customer.IsDeleted = !customer.IsDeleted;
                customer.Status = customer.IsDeleted ? "Inactive" : "Active";
                customer.UpdatedAt = DateTime.Now;

                var success = await _accountService.UpdateAsync(id, customer);
                TempData["Success"] = success
                    ? (customer.IsDeleted ? "Đã vô hiệu hoá khách hàng!" : "Đã kích hoạt lại khách hàng!")
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
            var allCustomers = await _accountService.GetAllCustomerAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                allCustomers = allCustomers.Where(c =>
                    (c.AccountName?.ToLower().Contains(kw) ?? false) ||
                    (c.Email?.ToLower().Contains(kw) ?? false) ||
                    (c.PhoneNumber?.Contains(kw) ?? false)
                ).ToList();
            }

            ViewBag.EditCustomer = editModel;
            ViewBag.Query = q;
            return View("~/Views/Admin/ManageAccountCustomer.cshtml",
                allCustomers.OrderByDescending(c => c.CreatedAt).ToList());
        }
    }
}
