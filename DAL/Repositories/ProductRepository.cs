// DAL/Repositories/ProductRepository.cs
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AspLorKingDomContext _db;

        public ProductRepository(AspLorKingDomContext db)
        {
            _db = db;
        }

        public async Task<List<(int Id, string Name)>> GetBasicListAsync(string? keyword = null, int limit = 200)
        {
            IQueryable<Product> q = _db.Products.AsNoTracking();

            // Nếu có cột IsDeleted:
            // q = q.Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = $"%{keyword.Trim()}%";
                q = q.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, kw));
            }

            // 1) Select sang kiểu dịch được (ProductLite)
            // 2) ToListAsync() (thực thi SQL)
            // 3) Map sang tuple trên bộ nhớ
            var rows = await q
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductLite { Id = p.ProductId, Name = p.ProductName! })
                .Take(limit)
                .ToListAsync();

            return rows.Select(r => (r.Id, r.Name)).ToList();
        }

        // DTO tạm để EF dịch ra SQL
        private sealed class ProductLite
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
        }


        public Task<bool> ExistsAsync(int productId)
        {
            return _db.Products
                .AsNoTracking()
                .AnyAsync(p => p.ProductId == productId /* && !p.IsDeleted */);
        }
    }
}
