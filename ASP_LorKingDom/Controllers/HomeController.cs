using BLL.Interfaces;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ASP_LorKingDom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productSvc;
        public HomeController(ILogger<HomeController> logger, IProductService productSvc) // <- thêm service
        {
            _logger = logger;
            _productSvc = productSvc;
        }



        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 16)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            var all = await _productSvc.GetAllAsync(q);

            var filtered = all.Where(p =>
                !p.IsDeleted &&
                p.StockQuantity > 0 &&
                p.ProductStatus == "Available" &&
                p.CategoryId != null &&
                p.MaterialId != null &&
                p.AgeId != null &&
                p.SexId != null &&
                p.PriceRangeId != null &&
                p.BrandId != null &&
                p.OriginId != null
            );

            var total = filtered.Count();

            var items = filtered
                .OrderByDescending(p => p.CreatedAt)   
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new BLL.DTOs.PagedResult<BLL.DTOs.ProductDto>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Cart()
        {
            return View();
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
