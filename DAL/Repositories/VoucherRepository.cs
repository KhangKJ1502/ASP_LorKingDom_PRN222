using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly AspLorKingDomContext _context;

        public VoucherRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Voucher>> GetAllAsync()
        {
            return await _context.Vouchers
                .Include(v => v.VoucherType)
                .Include(v => v.CreateByNavigation)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<Voucher?> GetByIdAsync(int voucherId)
        {
            return await _context.Vouchers
                .Include(v => v.VoucherType)
                .Include(v => v.CreateByNavigation)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId);
        }

        public async Task<int> CreateAsync(Voucher voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher.VoucherId;
        }

        public async Task<bool> UpdateAsync(Voucher voucher)
        {
            var existingVoucher = await _context.Vouchers.FindAsync(voucher.VoucherId);
            if (existingVoucher == null)
                return false;

            _context.Entry(existingVoucher).CurrentValues.SetValues(voucher);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VoucherCodeExistsAsync(string voucherCode, int? excludeVoucherId = null)
        {
            var query = _context.Vouchers.Where(v => v.VoucherCode == voucherCode);
            if (excludeVoucherId.HasValue)
                query = query.Where(v => v.VoucherId != excludeVoucherId.Value);
            return await query.AnyAsync();
        }
    }
}