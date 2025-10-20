using DAL.Models;

namespace DAL.Interfaces
{
    public interface IEmailOtpRepository
    {
        Task<EmailOtp?> GetByEmailAndPurposeAsync(string email, string purpose);
        Task AddAsync(EmailOtp entity);
        Task UpdateAsync(EmailOtp entity);
        Task<bool> ExistsActiveOtpAsync(string email, string purpose);
    }
}
