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
        public async Task UpsertImagesAsync(
     int productId,
     string? mainImageUrl,
     IEnumerable<string> keepSecondaryUrls,
     IEnumerable<string> addSecondaryUrls,
     bool keepMainIfNull = true)
        {
            if (productId <= 0) throw new ArgumentException("ProductId không hợp lệ.");

            var keepSet = new HashSet<string>((keepSecondaryUrls ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var addList = (addSecondaryUrls ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            await _repo.ExecuteInTransactionAsync(async () =>
            {
                // === Main image ===
                if (!keepMainIfNull || !string.IsNullOrWhiteSpace(mainImageUrl))
                {
                    // bỏ main cũ
                    await _repo.UnsetMainAsync(productId);

                    // set main mới nếu có
                    if (!string.IsNullOrWhiteSpace(mainImageUrl))
                    {
                        await _repo.AddAsync(new DAL.Models.ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = mainImageUrl.Trim(),
                            IsMain = true
                        });
                    }
                }

                // === Secondary images ===
                var current = await _repo.GetByProductIdAsync(productId);
                var currentSecondary = current.Where(x => !x.IsMain).ToList();

                // xóa những ảnh phụ KHÔNG nằm trong keepSet
                var toRemove = currentSecondary
                    .Where(x => !keepSet.Contains(x.ImageUrl ?? ""))
                    .ToList();
                if (toRemove.Count > 0)
                    _repo.RemoveRange(toRemove);   // ⬅️ thêm method này ở repo (ở dưới)

                // số slot còn lại
                var remained = Math.Max(0, MAX_SECONDARY - (currentSecondary.Count - toRemove.Count));

                if (remained > 0)
                {
                    var toAdd = addList
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(remained)
                        .Select(url => new DAL.Models.ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = url,
                            IsMain = false
                        })
                        .ToList();

                    if (toAdd.Count > 0) await _repo.AddRangeAsync(toAdd);
                }

                await _repo.SaveChangesAsync();   // ⬅️ thêm method ở repo (ở dưới)
            });
        }
    }
}
