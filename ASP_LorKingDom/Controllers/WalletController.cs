using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace WebUI.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly IWalletService _walletService;
        private readonly IAccountService _accountService;
        private readonly ICompositeViewEngine _viewEngine;

        public WalletController(
            IWalletService walletService,
            IAccountService accountService,
            ICompositeViewEngine viewEngine)
        {
            _walletService = walletService;
            _accountService = accountService;
            _viewEngine = viewEngine;
        }

        private int GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Partial()
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0) return Unauthorized();

            var wallet = await _walletService.GetByAccountIdAsync(accountId);
            // Gọi theo quy ước: Views/Wallet/Wallets.cshtml
            return PartialView("Wallets", wallet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateWalletDto dto)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var walletId = await _walletService.CreateAsync(accountId, dto.WalletName);
                var wallet = await _walletService.GetByAccountIdAsync(accountId);

                var html = await RenderPartialToStringAsync("Wallets", wallet);
                return Json(new { success = true, html });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUp([FromBody] TopUpWalletDto dto)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var result = await _walletService.TopUpAsync(accountId, dto);

                if (result.Success)
                {
                    var wallet = await _walletService.GetByAccountIdAsync(accountId);
                    var html = await RenderPartialToStringAsync("Wallets", wallet);

                    return Json(new
                    {
                        success = true,
                        message = result.Message,
                        newBalance = result.NewBalance,
                        html = html
                    });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private async Task<string> RenderPartialToStringAsync(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.ActionDescriptor.ActionName;

            ViewData.Model = model;

            using var writer = new StringWriter();

            // Ưu tiên tìm theo quy ước MVC
            var viewResult = _viewEngine.FindView(ControllerContext, viewName, isMainPage: false);

            // Fallback: nếu tham số là absolute path
            if (!viewResult.Success)
            {
                viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
            }

            if (!viewResult.Success || viewResult.View == null)
            {
                var locations = string.Join(", ", viewResult.SearchedLocations ?? Enumerable.Empty<string>());
                throw new FileNotFoundException($"Không tìm thấy view: {viewName}. Đã tìm ở: {locations}");
            }

            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.GetStringBuilder().ToString();
        }
    }
}
