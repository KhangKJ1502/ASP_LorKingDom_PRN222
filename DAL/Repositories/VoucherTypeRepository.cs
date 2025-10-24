using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public interface IVoucherTypeRepository
    {
        Task<List<VoucherType>> GetAllAsync();
    }

    public class VoucherTypeRepository : IVoucherTypeRepository
    {
        private readonly AspLorKingDomContext _context;

        public VoucherTypeRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<VoucherType>> GetAllAsync()
        {
            return await _context.VoucherTypes
                .OrderBy(vt => vt.VoucherTypeName)
                .ToListAsync();
        }
    }
}