using DAL.Models;

namespace DAL.Interfaces
{
    public interface IAccountRepository
    {
        /// <summary>
        /// Lấy tất cả tài khoản Active, chưa bị xoá (phục vụ gửi All).
        /// </summary>
        Task<List<Account>> GetAllAsync();

        /// <summary>
        /// Lấy các tài khoản theo Role, chỉ Active và chưa xoá.
        /// </summary>
        Task<List<Account>> GetByRoleIdAsync(int roleId);

        /// <summary>
        /// Lấy 1 account theo id (AsNoTracking).
        /// </summary>
        Task<Account?> GetByIdAsync(int accountId);

        Task<Account?> GetByEmailAsync(string email);

        Task AddAsync(Account entity);

        Task UpdateAsync(Account entity);

        Task<bool> ExistsByEmailAsync(string email);
    }
}
