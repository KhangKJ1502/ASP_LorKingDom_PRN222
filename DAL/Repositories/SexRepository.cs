using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class SexRepository : ISexRepository
    {
        private readonly AspLorKingDomContext _context;

        public SexRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Sex>> GetActiveAsync()
        {
            return await _context.Sexes
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SexName)
                .ToListAsync();
        }
        public async Task<Sex?> GetByIdAsync(int id)
        {
            return await _context.Sexes.FirstOrDefaultAsync(s => s.SexId == id);
        }
    }
}
