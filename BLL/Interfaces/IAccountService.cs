using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAllAsync();
        Task<List<AccountDto>> GetAllCustomerAsync();
        Task<List<AccountDto>> GetAllStaffAsync();
        Task<List<AccountDto>> GetByRoleIdAsync(int roleId);

        Task<AccountDto?> GetByEmailAsync(string email);
        Task<AccountDto?> GetByIdAsync(int id);

        Task<int> CreateAsync(AccountDto dto);
        Task<bool> UpdateAsync(int id, AccountDto dto);
        Task<bool> ExistsByEmailAsync(string email);

        Task<AccountDto?> AuthenticateAsync(string email, string password);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
    }
}
