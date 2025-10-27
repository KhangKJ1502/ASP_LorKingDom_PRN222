using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _repo;
        private readonly IProductRepository _productRepo;

        public WishlistService(IWishlistRepository repo, IProductRepository productRepo)
        {
            _repo = repo;
            _productRepo = productRepo;
        }

        public async Task<(bool added, int count)> ToggleAsync(int accountId, int productId)
        {
            // xác nhận product còn tồn tại
            var p = await _productRepo.GetByIdAsync(productId);
            if (p == null) throw new ArgumentException("Sản phẩm không tồn tại.");

            var exists = await _repo.ExistsAsync(accountId, productId);
            if (exists)
                await _repo.DeleteAsync(accountId, productId);
            else
                await _repo.AddAsync(accountId, productId);

            var count = await _repo.CountAsync(accountId);
            return (!exists, count);
        }

        public Task<int> RemoveAsync(int accountId, int productId) =>
            _repo.DeleteAsync(accountId, productId);

        public Task<int> ClearAsync(int accountId) =>
            _repo.DeleteAllAsync(accountId);

        public async Task<List<WishlistItemDto>> ListAsync(int accountId, string? keyword)
        {
            var list = await _repo.GetWithProductsAsync(accountId, keyword);
            return list.Select(w => new WishlistItemDto
            {
                ProductId = w.ProductId,
                ProductName = w.Product?.ProductName ?? "",
                Sku = w.Product?.Sku,
                Price = w.Product?.Price ?? 0,
                MainImageUrl = w.Product?.ProductImages?.FirstOrDefault(pi => pi.IsMain)?.ImageUrl,
                ProductStatus = w.Product?.ProductStatus ?? "Available",
                CreatedAt = w.CreatedAt
            }).ToList();
        }

        public Task<List<int>> GetProductIdsAsync(int accountId) =>
            _repo.GetProductIdsAsync(accountId);

        public Task<int> CountAsync(int accountId) =>
            _repo.CountAsync(accountId);
    }
}
