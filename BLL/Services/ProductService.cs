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
        private readonly IPromotionRepository _promotionRepo;

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
            IPromotionRepository promotionRepo,
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
            _promotionRepo = promotionRepo;

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

            var status = ProductValidator.NormalizeStatus(dto.ProductStatus);
            if (status == "Discontinued")
            {
                // tuỳ policy của bạn:
                dto.StockQuantity = 0;   // ngừng kinh doanh thì về 0
                                         // có thể đặt IsDeleted = true ở entity nếu muốn ẩn khỏi storefront
            }
            else
            {
                status = (dto.StockQuantity > 0) ? "Available" : "OutOfStock";
            }
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
                ProductStatus = status,
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
            e.Description = dto.DescriptionHtml;
            e.UpdatedAt = DateTime.Now;

            var status = ProductValidator.NormalizeStatus(dto.ProductStatus);

            if (status == "Discontinued")
            {
                e.ProductStatus = "Discontinued";
                e.IsDeleted = true;      // (tuỳ chính sách: có thể để false nếu không muốn ẩn khỏi storefront)
                e.Quantity = 0;          // (tuỳ: nhiều hệ thống cho về 0 khi ngừng kinh doanh)
            }
            else
            {
                e.IsDeleted = false;
                e.ProductStatus = (dto.StockQuantity > 0) ? "Available" : "OutOfStock";
            }


            await _repo.UpdateAsync(e);

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
            ,
            // Promotion mapping - only show if promotion is active and valid
            PromotionId = IsPromotionValid(x.Promotion) ? x.PromotionId : null,
            PromotionCode = IsPromotionValid(x.Promotion) ? x.Promotion?.PromotionCode : null,
            PromotionDiscountPercent = IsPromotionValid(x.Promotion) ? x.Promotion?.DiscountPercent : null,
            IsOnSale = IsPromotionValid(x.Promotion) && x.PromotionId != null
        };

        /// <summary>
        /// Kiểm tra promotion có hợp lệ không (Active, chưa xóa, trong thời gian)
        /// </summary>
        private static bool IsPromotionValid(DAL.Models.Promotion? promotion)
        {
            if (promotion == null) return false;
            if (promotion.IsDeleted) return false;
            if (promotion.Status != "Active") return false;
            
            var now = DateTime.Now;
            if (now < promotion.StartDate || now > promotion.EndDate) return false;
            
            return true;
        }

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
        public async Task<PagedResult<ProductDto>> GetAdminPagedAsync(string? keyword, int page, int pageSize)
        {
            var (items, total) = await _repo.QueryAdminPagedAsync(keyword, page, pageSize);
            return new PagedResult<ProductDto>
            {
                Items = items.Select(Map).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> SetPromotionAsync(int productId, int? promotionId)
        {
            if (productId <= 0) throw new ArgumentException("Invalid productId", nameof(productId));

            var product = await _repo.GetByIdAsync(productId);
            if (product == null) return false;

            // Nếu muốn gán promotion, kiểm tra promotion có hợp lệ không
            if (promotionId.HasValue)
            {
                var promotion = await _promotionRepo.GetByIdAsync(promotionId.Value);
                if (promotion == null)
                    throw new ArgumentException("Promotion không tồn tại", nameof(promotionId));
                
                if (promotion.IsDeleted)
                    throw new InvalidOperationException("Không thể gán promotion đã bị xóa");
                
                if (promotion.Status != "Active")
                    throw new InvalidOperationException("Chỉ có thể gán promotion đang Active");
                
                var now = DateTime.Now;
                if (now < promotion.StartDate)
                    throw new InvalidOperationException($"Promotion chưa bắt đầu (ngày bắt đầu: {promotion.StartDate:dd/MM/yyyy})");
                
                if (now > promotion.EndDate)
                    throw new InvalidOperationException($"Promotion đã hết hạn (ngày kết thúc: {promotion.EndDate:dd/MM/yyyy})");
            }

            product.PromotionId = promotionId;
            product.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(product);
            return true;
        }


    }
}
