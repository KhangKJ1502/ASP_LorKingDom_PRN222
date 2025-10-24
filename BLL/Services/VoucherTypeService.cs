using BLL.DTOs;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public interface IVoucherTypeService
    {
        Task<List<VoucherTypeDto>> GetAllAsync();
    }

    public class VoucherTypeService : IVoucherTypeService
    {
        private readonly AspLorKingDomContext _context;

        public VoucherTypeService(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<VoucherTypeDto>> GetAllAsync()
        {
            return await _context.VoucherTypes
                .Select(vt => new VoucherTypeDto
                {
                    VoucherTypeId = vt.VoucherTypeId,
                    VoucherTypeName = vt.VoucherTypeName
                })
                .OrderBy(vt => vt.VoucherTypeName)
                .ToListAsync();
        }
    }

    public class VoucherTypeDto
    {
        public int VoucherTypeId { get; set; }
        public string? VoucherTypeName { get; set; }
    }
}