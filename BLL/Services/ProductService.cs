using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;
using System.Security.Cryptography;
using System.Text;

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
            ProductValidator.ValidateForCreate(dto);
            var name = dto.ProductName.Trim();
            if (await _repo.ExistsByNameAsync(name))
                throw new ArgumentException("Tên sản phẩm đã tồn tại, vui lòng chọn tên khác.");

         
            var sku = await GenerateUniqueSkuAsync();

            var entity = new Product
            {
                Sku = sku,
                ProductName = name,
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

            //// ✅ Kiểm tra tất cả FK cha trong 1 lần (nếu product sẽ ở trạng thái hoạt động)
            //await EnsureParentsActiveIfProductActiveAsync(dto);

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
            MaterialName = x.Material?.MaterialName,      // đổi theo field thực tế
            AgeRange = x.Age?.AgeRange,               // hoặc AgeName
            SexName = x.Sex?.SexName,
            OriginName = x.Origin?.OriginName,

            MainImageUrl = x.ProductImages?.FirstOrDefault(pi => pi.IsMain)?.ImageUrl,
            SecondaryImageUrls = x.ProductImages?
                              .Where(pi => !pi.IsMain)
                              .Select(pi => pi.ImageUrl)
                              .Where(u => !string.IsNullOrWhiteSpace(u))
                              .ToList() ?? new List<string>()
        };

        public async Task<PagedResult<ProductDto>> GetStorefrontPagedAsync(string? keyword, int page, int pageSize)
        {
            // Gọi xuống repo để lọc + phân trang ngay trong DB (tối ưu)
            var (items, total) = await _repo.QueryStorefrontPagedAsync(keyword, page, pageSize);
            return new PagedResult<ProductDto>
            {
                Items = items.Select(Map).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

    }
}
