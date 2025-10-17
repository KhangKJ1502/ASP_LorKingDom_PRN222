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
    public class AgeRepository : IAgeRepository
    {
        private readonly AspLorKingDomContext _context;

        public AgeRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Age>> GetActiveAsync()
        {
            return await _context.Ages
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.AgeRange)
                .ToListAsync();
        }
    }
}
