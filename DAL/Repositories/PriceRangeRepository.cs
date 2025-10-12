using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class PriceRangeRepository : IPriceRangeRepository
    {
        private readonly AspLorKingDomContext _context;

        public PriceRangeRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<PriceRange>> GetAllAsync(string? keyword)
        {
            var q = _context.PriceRanges.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // 1) Thử parse "a - b"
                if (TryParseRange(keyword, out var iMin, out var iMax))
                {
                    // Lấy các khoảng GIAO với [iMin, iMax]
                    q = q.Where(x => !(x.PriceRangeMax < iMin || x.PriceRangeMin > iMax));
                }
                // 2) Thử parse 1 số
                else if (TryParseNumber(keyword, out var value))
                {
                    // Bao chứa hoặc trùng biên
                    q = q.Where(x =>
                        (x.PriceRangeMin <= value && x.PriceRangeMax >= value) ||
                        x.PriceRangeMin == value || x.PriceRangeMax == value);
                }
                // 3) Nếu nhập text lạ → không thêm điều kiện (để ra full / hoặc bạn có thể .Where(false))
            }

            return await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }


        public async Task<List<PriceRange>> GetActiveAsync()
        {
            return await _context.PriceRanges
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.PriceRangeMin)
                .ToListAsync();
        }

        public async Task<List<PriceRange>> SearchAsync(string keyword, bool includeDeleted = false)
        {
            var q = _context.PriceRanges.AsQueryable();

            if (!includeDeleted)
                q = q.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                if (TryParseRange(keyword, out var iMin, out var iMax))
                {
                    q = q.Where(x => !(x.PriceRangeMax < iMin || x.PriceRangeMin > iMax));
                }
                else if (TryParseNumber(keyword, out var value))
                {
                    q = q.Where(x =>
                        (x.PriceRangeMin <= value && x.PriceRangeMax >= value) ||
                        x.PriceRangeMin == value || x.PriceRangeMax == value);
                }
            }

            return await q.OrderBy(x => x.PriceRangeMin).ToListAsync();
        }

        // ================= helpers =================

        // Hỗ trợ "100,000 - 300000" / "100000-300000" (dấu phẩy, chấm, khoảng trắng không quan trọng)
        private static bool TryParseRange(string input, out decimal min, out decimal max)
        {
            min = max = 0;
            var parts = Regex.Split(input ?? "", @"\-|to|–|—", RegexOptions.IgnoreCase)
                             .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            if (parts.Length != 2) return false;

            return TryParseNumber(parts[0], out min) && TryParseNumber(parts[1], out max) && min <= max;
        }

        // Nhận "500,000", "500.000", "500000" → 500000
        private static bool TryParseNumber(string input, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Bỏ mọi ký tự không phải số hoặc dấu thập phân
            // Ở VN, thường dấu phẩy là phân tách nghìn, ta loại bỏ , . cách
            var cleaned = Regex.Replace(input, @"[^\d]", "");
            if (cleaned.Length == 0) return false;

            // Parse theo Invariant vì ta đã bỏ phân tách nghìn
            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        public async Task<PriceRange?> GetByIdAsync(int id)
        {
            return await _context.PriceRanges.FirstOrDefaultAsync(x => x.PriceRangeId == id);
        }

        public async Task AddAsync(PriceRange entity)
        {
            await _context.PriceRanges.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PriceRange entity)
        {
            _context.PriceRanges.Update(entity);
            await _context.SaveChangesAsync();
        }

        // Kiểm tra trùng (cùng min và max)
        public async Task<bool> ExistsAsync(decimal min, decimal max, int? excludeId = null)
        {
            return await _context.PriceRanges.AnyAsync(x =>
                x.PriceRangeMin == min &&
                x.PriceRangeMax == max &&
                (excludeId == null || x.PriceRangeId != excludeId));
        }
    }
}
