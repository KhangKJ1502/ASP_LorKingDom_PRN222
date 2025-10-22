using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class ProductImageService : IProductImageService
    {
        private const int MAX_SECONDARY = 6;
        private readonly IProductImageRepository _repo;

        public ProductImageService(IProductImageRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> AddImagesAsync(int productId, string mainImageUrl, IEnumerable<string> secondaryImageUrls)
        {
            if (productId <= 0) throw new ArgumentException("ProductId không hợp lệ.");
            mainImageUrl = (mainImageUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(mainImageUrl))
                throw new ArgumentException("Ảnh main là bắt buộc.");

            // Ép thành List<string> để dùng Count (property) an toàn
            var secondaryList = (secondaryImageUrls ?? Enumerable.Empty<string>())
                .Select(s => (s ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            int actuallyAddedSecondary = 0;

            await _repo.ExecuteInTransactionAsync(async () =>
            {
                // 1) Bỏ main cũ
                await _repo.UnsetMainAsync(productId);

                // 2) Thêm main mới  (LƯU Ý: ProductId, KHÔNG phải ProductID)
                var main = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = mainImageUrl,
                    IsMain = true
                };
                await _repo.AddAsync(main);

                // 3) Giới hạn ảnh phụ còn trống
                var existed = await _repo.CountSecondaryAsync(productId); // int
                var slots = Math.Max(0, MAX_SECONDARY - existed);

                if (slots > 0 && secondaryList.Count > 0) // Count property của List
                {
                    var toAdd = secondaryList
                        .Take(slots)
                        .Select(url => new ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = url,
                            IsMain = false
                        })
                        .ToList();

                    if (toAdd.Count > 0)
                    {
                        await _repo.AddRangeAsync(toAdd);
                        actuallyAddedSecondary = toAdd.Count;
                    }
                }
            });

            return actuallyAddedSecondary;
        }
    }
}
