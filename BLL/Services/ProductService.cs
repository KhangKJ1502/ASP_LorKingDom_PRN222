using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IProductImageService _imageSvc;

        // Repos cha để kiểm tra trạng thái hoạt động
        private readonly IBrandRepository _brandRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IMaterialRepository _materialRepo;
        private readonly IOriginRepository _originRepo;
        private readonly IAgeRepository _ageRepo;
        private readonly ISexRepository _sexRepo;
        private readonly IPriceRangeRepository _priceRangeRepo;

        public ProductService(
            IProductRepository repo,
            IProductImageService imageSvc,
            IBrandRepository brandRepo,
            ICategoryRepository categoryRepo,
            IMaterialRepository materialRepo,
            IOriginRepository originRepo,
            IAgeRepository ageRepo,
            ISexRepository sexRepo,
            IPriceRangeRepository priceRangeRepo)
        {
            _repo = repo;
            _imageSvc = imageSvc;

            _brandRepo = brandRepo;
            _categoryRepo = categoryRepo;
            _materialRepo = materialRepo;
            _originRepo = originRepo;
            _ageRepo = ageRepo;
            _sexRepo = sexRepo;
            _priceRangeRepo = priceRangeRepo;
        }

        public async Task<List<ProductDto>> GetAllAsync(string? keyword = null)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(ProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.Id = 0; // tạo mới
            ProductValidator.Validate(dto);

            var nameTrim = (dto.ProductName ?? "").Trim();
            if (await _repo.ExistsByNameAsync(nameTrim))
                throw new ArgumentException("Tên sản phẩm đã tồn tại, vui lòng chọn tên khác.");

            // ✅ Kiểm tra tất cả FK cha trong 1 lần (nếu product sẽ ở trạng thái hoạt động)
            await EnsureParentsActiveIfProductActiveAsync(dto);

            var sku = await GenerateUniqueSkuAsync();

            var entity = new Product
            {
                Sku = sku,
                ProductName = nameTrim,
                CategoryId = dto.CategoryId,
                MaterialId = dto.MaterialId,
                AgeId = dto.AgeId,
                SexId = dto.SexId,
                PriceRangeId = dto.PriceRangeId,
                BrandId = dto.BrandId,
                OriginId = dto.OriginId,
                Price = dto.Price,
                Quantity = dto.StockQuantity,
                ProductStatus = dto.StockQuantity == 0 ? "OutOfStock" : dto.ProductStatus,
                Description = dto.DescriptionHtml,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            await _repo.AddAsync(entity);

            // Ảnh
            var main = (dto.MainImageUrl ?? "").Trim();
            var secs = (dto.SecondaryImageUrls ?? new())
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0)
                .ToList();

            if (!string.IsNullOrWhiteSpace(main) || secs.Count > 0)
                await _imageSvc.AddImagesAsync(entity.ProductId, main, secs);

            return entity.ProductId;
        }

        public async Task<bool> UpdateAsync(ProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Id <= 0) throw new ArgumentException("ProductId không hợp lệ.");
            ProductValidator.Validate(dto);

            var nameTrim = (dto.ProductName ?? "").Trim();
            if (await _repo.ExistsByNameAsync(nameTrim, dto.Id))
                throw new ArgumentException("Tên sản phẩm đã tồn tại, vui lòng chọn tên khác.");

            var e = await _repo.GetByIdAsync(dto.Id);
            if (e == null) return false;

            // ✅ Kiểm tra tất cả FK cha trong 1 lần (nếu product sẽ ở trạng thái hoạt động)
            await EnsureParentsActiveIfProductActiveAsync(dto);

            e.ProductName = nameTrim;
            e.CategoryId = dto.CategoryId;
            e.MaterialId = dto.MaterialId;
            e.AgeId = dto.AgeId;
            e.SexId = dto.SexId;
            e.PriceRangeId = dto.PriceRangeId;
            e.BrandId = dto.BrandId;
            e.OriginId = dto.OriginId;
            e.Price = dto.Price;
            e.Quantity = dto.StockQuantity;
            e.ProductStatus = dto.StockQuantity == 0 ? "OutOfStock" : dto.ProductStatus;
            e.Description = dto.DescriptionHtml;
            e.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(e);

            // Ảnh
            var main = (dto.MainImageUrl ?? "").Trim();
            var secs = (dto.SecondaryImageUrls ?? new())
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0)
                .ToList();

            if (!string.IsNullOrWhiteSpace(main) || secs.Count > 0)
                await _imageSvc.AddImagesAsync(e.ProductId, main, secs);

            return true;
        }

        // =========================
        // Private helpers (FIX lỗi tuple)
        // =========================

        // Dùng type thay vì tuple để tránh lỗi collection initializer với lambda async
        private readonly struct ParentCheck
        {
            public int? Value { get; }
            public string Label { get; }
            public Func<int, Task<bool>> IsActive { get; }

            public ParentCheck(int? value, string label, Func<int, Task<bool>> isActive)
            {
                Value = value;
                Label = label;
                IsActive = isActive;
            }
        }

        // Quy ước: Available/OutOfStock = hoạt động; Discontinued = không hoạt động
        private static bool WillBeActive(ProductDto dto)
        {
            var status = dto.StockQuantity == 0 ? "OutOfStock" : (dto.ProductStatus ?? "Available");
            return !string.Equals(status, "Discontinued", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Nếu sản phẩm sẽ hoạt động, tất cả FK cha (nếu có) phải đang hoạt động.
        /// Nếu có cha nào ẩn, ném InvalidOperationException.
        /// </summary>
        private async Task EnsureParentsActiveIfProductActiveAsync(ProductDto dto)
        {
            if (!WillBeActive(dto)) return;

            var checks = new List<ParentCheck>
            {
                new ParentCheck(dto.BrandId,      "Thương hiệu",  async id => { var x = await _brandRepo.GetByIdAsync(id);      return x != null && !x.IsDeleted; }),
                new ParentCheck(dto.CategoryId,   "Danh mục",     async id => { var x = await _categoryRepo.GetByIdAsync(id);   return x != null && !x.IsDeleted; }),
                new ParentCheck(dto.MaterialId,   "Chất liệu",    async id => { var x = await _materialRepo.GetByIdAsync(id);   return x != null && !x.IsDeleted; }),
                new ParentCheck(dto.OriginId,     "Nguồn gốc",    async id => { var x = await _originRepo.GetByIdAsync(id);     return x != null && !x.IsDeleted; }),
                new ParentCheck(dto.AgeId,        "Khoảng tuổi",  async id => { var x = await _ageRepo.GetByIdAsync(id);        return x != null && !x.IsDeleted; }),
                new ParentCheck(dto.SexId,        "Giới tính",    async id => { var x = await _sexRepo.GetByIdAsync(id);        return x != null && !x.IsDeleted; }),
                new ParentCheck(dto.PriceRangeId, "Khoảng giá",   async id => { var x = await _priceRangeRepo.GetByIdAsync(id); return x != null && !x.IsDeleted; }),
            };

            foreach (var c in checks)
            {
                if (c.Value.HasValue)
                {
                    var ok = await c.IsActive(c.Value.Value);
                    if (!ok)
                        throw new InvalidOperationException($"Không thể đặt sản phẩm hoạt động vì {c.Label} đang không hoạt động.");
                }
            }
        }

        private async Task<string> GenerateUniqueSkuAsync()
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            using var rng = RandomNumberGenerator.Create();

            for (int attempt = 0; attempt < 10; attempt++)
            {
                var data = new byte[6];
                rng.GetBytes(data);
                var sb = new StringBuilder(6);
                foreach (var b in data) sb.Append(alphabet[b % alphabet.Length]);
                var sku = sb.ToString();
                if (!await _repo.ExistsBySkuAsync(sku))
                    return sku;
            }
            // fallback cực hiếm
            return $"SKU{DateTime.UtcNow.Ticks % 1_000_000:D6}";
        }

        private static ProductDto Map(Product x) => new()
        {
            Id = x.ProductId,
            Sku = x.Sku,
            ProductName = x.ProductName,
            CategoryId = x.CategoryId,
            MaterialId = x.MaterialId,
            AgeId = x.AgeId,
            SexId = x.SexId,
            PriceRangeId = x.PriceRangeId,
            BrandId = x.BrandId,
            OriginId = x.OriginId,
            Price = x.Price,
            StockQuantity = x.Quantity,
            ProductStatus = x.ProductStatus,
            DescriptionHtml = x.Description,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CategoryName = x.Category?.CategoryName,
            BrandName = x.Brand?.BrandName,
            MainImageUrl = x.ProductImages?.FirstOrDefault(pi => pi.IsMain)?.ImageUrl
        };
    }
}
