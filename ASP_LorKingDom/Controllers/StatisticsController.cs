using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace ASP_LorKingDom.Controllers
{
    //[Authorize(AuthenticationSchemes = "AdminScheme", Roles = "Admin,Staff,Warehouse")]
    public class StatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        // Admin và Staff được xem Dashboard (doanh thu), Warehouse không được xem
        [HttpGet]
        //[AdminAndStaffOnly]
        public async Task<IActionResult> Dashboard()
        {
            // Kiểm tra quyền: chỉ Admin và Staff được xem
            if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("ProductStatistics");
            }

            var stats = await _statisticsService.GetDashboardStatisticsAsync();
            return View("~/Views/Admin/Dashboard.cshtml", stats);
        }

        // Admin và Warehouse được xem Product Statistics (sản phẩm), Staff không được xem
        [HttpGet]
        //[AdminAndWarehouseOnly]
        public async Task<IActionResult> ProductStatistics()
        {
            // Kiểm tra quyền: chỉ Admin và Warehouse được xem
            if (!User.IsInRole("Admin") && !User.IsInRole("Warehouse"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Dashboard");
            }

            var stats = await _statisticsService.GetProductStatisticsAsync();
            return View("~/Views/Admin/ProductStatistics.cshtml", stats);
        }
    }
}
