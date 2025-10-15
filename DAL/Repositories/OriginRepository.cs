using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class OriginRepository : IOriginRepository
    {
        private readonly AspLorKingDomContext _context;

        public OriginRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<Origin>> GetAllAsync(string? keyword)
        {
            var query = _context.Origins.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.OriginName.Contains(keyword));

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Origin>> GetActiveAsync()
        {
            return await _context.Origins
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.OriginName)
                .ToListAsync();
        }

        public async Task<Origin?> GetByIdAsync(int id)
        {
            return await _context.Origins.FirstOrDefaultAsync(x => x.OriginId == id);
        }

        public async Task AddAsync(Origin entity)
        {
            await _context.Origins.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Origin entity)
        {
            _context.Origins.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            return await _context.Origins.AnyAsync(x => x.OriginName == name &&
                              (excludeId == null || x.OriginId != excludeId));
        }
    }
}
