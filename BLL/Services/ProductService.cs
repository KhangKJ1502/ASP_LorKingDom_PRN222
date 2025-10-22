using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IProductImageService _imageSvc;

        public ProductService(IProductRepository repo, IProductImageService imageSvc)
        {
            _repo = repo;
            _imageSvc = imageSvc;
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
            ProductValidator.ValidateForCreate(dto);
            var name = dto.ProductName.Trim();
            if (await _repo.ExistsByNameAsync(name))
                throw new ArgumentException("Tên sản phẩm đã tồn tại, vui lòng chọn tên khác.");

            var sku = await GenerateUniqueSkuAsync();

            var entity = new Product
            {
                Sku = sku,
                ProductName = dto.ProductName.Trim(),
                CategoryId = dto.CategoryId,
                MaterialId = dto.MaterialId,
                AgeId = dto.AgeId,
                SexId = dto.SexId,
                PriceRangeId = dto.PriceRangeId,
                BrandId = dto.BrandId,
                OriginId = dto.OriginId,
                Price = dto.Price,
                Quantity = dto.StockQuantity,
                ProductStatus = dto.ProductStatus,
                Description = dto.DescriptionHtml,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            await _repo.AddAsync(entity);

            // Ảnh (khi Controller đã cung cấp URL)
            var main = (dto.MainImageUrl ?? "").Trim();
            var secs = (dto.SecondaryImageUrls ?? new())
                .Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).ToList();

            if (!string.IsNullOrWhiteSpace(main) || secs.Count > 0)
                await _imageSvc.AddImagesAsync(entity.ProductId, main, secs);

            return entity.ProductId;
        }

        public async Task<bool> UpdateAsync(ProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Id <= 0) throw new ArgumentException("ProductId không hợp lệ.");
            ProductValidator.Validate(dto);
            var name = dto.ProductName.Trim();
            if (await _repo.ExistsByNameAsync(dto.ProductName, dto.Id))
                throw new ArgumentException("Tên sản phẩm đã tồn tại, vui lòng chọn tên khác.");

            var e = await _repo.GetByIdAsync(dto.Id);
            if (e == null) return false;

            e.ProductName = dto.ProductName.Trim();
            e.CategoryId = dto.CategoryId;
            e.MaterialId = dto.MaterialId;
            e.AgeId = dto.AgeId;
            e.SexId = dto.SexId;
            e.PriceRangeId = dto.PriceRangeId;
            e.BrandId = dto.BrandId;
            e.OriginId = dto.OriginId;
            e.Price = dto.Price;
            e.Quantity = dto.StockQuantity;
            if (dto.StockQuantity == 0)
            {
                e.ProductStatus = "OutOfStock";
            }
            else
            {
                e.ProductStatus = dto.ProductStatus;
            }

            e.Description = dto.DescriptionHtml;
            e.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(e);

            var main = (dto.MainImageUrl ?? "").Trim();
            var secs = (dto.SecondaryImageUrls ?? new())
                .Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).ToList();

            if (!string.IsNullOrWhiteSpace(main) || secs.Count > 0)
                await _imageSvc.AddImagesAsync(e.ProductId, main, secs);

            return true;
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
