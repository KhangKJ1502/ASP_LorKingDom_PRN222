using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class EmailOtpRepository : IEmailOtpRepository
    {
        private readonly AspLorKingDomContext _context;

        public EmailOtpRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<EmailOtp?> GetByEmailAndPurposeAsync(string email, string purpose)
        {
            return await _context.EmailOtps
                .FirstOrDefaultAsync(x => x.Email == email && x.Purpose == purpose && !x.IsUsed && x.ExpiresAt > DateTime.Now);
        }

        public async Task AddAsync(EmailOtp entity)
        {
            await _context.EmailOtps.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmailOtp entity)
        {
            _context.EmailOtps.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsActiveOtpAsync(string email, string purpose)
        {
            return await _context.EmailOtps
                .AnyAsync(x => x.Email == email && x.Purpose == purpose && !x.IsUsed && x.ExpiresAt > DateTime.Now);
        }
    }
}
