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
        /// Lấy tất cả khách hàng (RoleId = 4), Active, chưa xoá.
        /// </summary>
        Task<List<Account>> GetAllCustomerAsync();

        /// <summary>
        /// Lấy tất cả nhân viên (RoleId = 2 hoặc 3), Active, chưa xoá.
        /// </summary>
        Task<List<Account>> GetAllStaffAsync();

        /// <summary>
        /// Lấy các tài khoản theo Role, chỉ Active và chưa xoá.
        /// </summary>
        Task<List<Account>> GetByRoleIdAsync(int roleId);

        /// <summary>
        /// Lấy 1 account theo id (AsNoTracking).
        /// </summary>
        Task<Account?> GetByIdAsync(int accountId);

        /// <summary>
        /// Lấy 1 account theo email.
        /// </summary>
        Task<Account?> GetByEmailAsync(string email);

        /// <summary>
        /// Thêm tài khoản mới.
        /// </summary>
        Task AddAsync(Account entity);

        /// <summary>
        /// Cập nhật tài khoản.
        /// </summary>
        Task UpdateAsync(Account entity);

        /// <summary>
        /// Kiểm tra xem email đã tồn tại chưa.
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email);
    }
}
