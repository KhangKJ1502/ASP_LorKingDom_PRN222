using BLL.DTOs;
using BLL.Interfaces;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace ASP_LorKingDom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productSvc;
        private readonly IWishlistService _wishlistSvc;

        public HomeController(
            ILogger<HomeController> logger,
            IProductService productSvc,
            IWishlistService wishlistSvc)
        {
            _logger = logger;
            _productSvc = productSvc;
            _wishlistSvc = wishlistSvc;
        }

        // ===== helpers =====
        private async Task<HashSet<int>> GetLikedSetAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return new();
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idStr, out var accountId)
                ? new HashSet<int>(await _wishlistSvc.GetProductIdsAsync(accountId))
                : new HashSet<int>();
        }

        private async Task<PagedResult<ProductDto>> BuildPagedModelAsync(string? q, int page, int pageSize)
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

            var liked = await GetLikedSetAsync();
            foreach (var p in items) p.IsLiked = liked.Contains(p.Id);

            return new PagedResult<ProductDto>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

     
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 16)
        {
            var model = await BuildPagedModelAsync(q, page, pageSize);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ProductGrid(string? q, int page = 1, int pageSize = 16)
        {
            var model = await BuildPagedModelAsync(q, page, pageSize);
            return PartialView("_ProductGridPartial", model);
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            var dto = await _productSvc.GetByIdAsync(id);
            if (dto == null) return NotFound();
            var liked = await GetLikedSetAsync();
            dto.IsLiked = liked.Contains(id);
            return View(dto);
        }

        public IActionResult Privacy() => View();
        public IActionResult Contact() => View();
        public IActionResult Cart() => View();

        [Authorize]
        public IActionResult Profile() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
