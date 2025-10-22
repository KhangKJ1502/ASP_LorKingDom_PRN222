using DAL.Models;

namespace DAL.Interfaces
{
    public interface IAccountRepository
    {
        // Public lists – Active & !Deleted
        Task<List<Account>> GetAllAsync();
        Task<List<Account>> GetAllCustomerAsync();
        Task<List<Account>> GetAllStaffAsync();
        Task<List<Account>> GetByRoleIdAsync(int roleId);

        // Admin lists – KHÔNG filter
        Task<List<Account>> AdminGetAllStaffAsync();
        Task<List<Account>> AdminGetAllCustomerAsync();

        Task<Account?> GetByIdAsync(int accountId);
        Task<Account?> GetByEmailAsync(string email);

        Task AddAsync(Account entity);
        Task UpdateAsync(Account entity);

        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByPhoneAsync(string phoneNumber, int? excludeId = null);
    }
}
