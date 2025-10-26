// DAL/Repositories/PromotionRepository.cs
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly AspLorKingDomContext _context;

        public PromotionRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Promotion>> GetAllAsync(string? keyword)
        {
            var query = _context.Promotions
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = $"%{keyword.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.PromotionCode, kw) ||
                    (p.Description != null && EF.Functions.Like(p.Description, kw)));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<Promotion?> GetByIdAsync(int id)
        {
            return _context.Promotions
                .FirstOrDefaultAsync(p => p.PromotionId == id && !p.IsDeleted);
        }

        public async Task<Promotion> AddAsync(Promotion promotion)
        {
            promotion.CreatedAt = DateTime.Now;
            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }

        public async Task<bool> UpdateAsync(Promotion promotion)
        {
            var existing = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromotionId == promotion.PromotionId);
            if (existing == null) return false;

            existing.PromotionCode = promotion.PromotionCode;
            existing.Description = promotion.Description;
            existing.DiscountPercent = promotion.DiscountPercent;
            existing.StartDate = promotion.StartDate;
            existing.EndDate = promotion.EndDate;
            existing.Status = promotion.Status;
            existing.UpdatedAt = DateTime.Now;

                
            try
            {
                var affected = await _context.SaveChangesAsync();
                return affected > 0;
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                Console.WriteLine($"[ERROR] UpdateAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promo == null) return false;

            promo.IsDeleted = true;
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promo == null) return false;

            promo.IsDeleted = false;
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatusAsync(int id, string status)
        {
            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promo == null) return false;

            promo.Status = status;
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promo == null) return false;

            promo.Status = promo.Status == "Active" ? "Inactive" : "Active";
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.Promotions
                .Where(p => !p.IsDeleted
                            && p.Status == "Active"
                            && p.StartDate <= now
                            && p.EndDate >= now)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<List<Promotion>> GetActiveByProductAsync(int productId)
        {
            var now = DateTime.Now;
            return _context.Promotions
                .Where(p => !p.IsDeleted
                            && p.Status == "Active"
                            && p.StartDate <= now
                            && p.EndDate >= now
                            && p.Products.Any(pr => pr.ProductId == productId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<bool> ExistsByNameAsync(string code, int? excludeId = null)
        {
            var query = _context.Promotions.Where(p => p.PromotionCode == code && !p.IsDeleted);
            if (excludeId.HasValue)
                query = query.Where(p => p.PromotionId != excludeId.Value);
            return query.AnyAsync();
        }

        public Task<bool> HasOverlapAsync(int promotionId, DateTime start, DateTime end, int? excludeId = null)
        {
            if (end < start) (start, end) = (end, start);

            var query = _context.Promotions
                .Where(p => !p.IsDeleted);

            if (excludeId.HasValue)
                query = query.Where(p => p.PromotionId != excludeId.Value);

            return query.AnyAsync(p => p.StartDate <= end && p.EndDate >= start);
        }
    }
}