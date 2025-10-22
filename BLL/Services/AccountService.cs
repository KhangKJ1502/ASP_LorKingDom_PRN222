using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;
        public AccountService(IAccountRepository accountRepo) => _accountRepo = accountRepo;

        // ===== GETs (Public) =====
        public async Task<List<AccountDto>> GetAllAsync() =>
            (await _accountRepo.GetAllAsync()).Select(Map).ToList();

        public async Task<List<AccountDto>> GetAllCustomerAsync() =>
            (await _accountRepo.GetAllCustomerAsync()).Select(Map).ToList();

        public async Task<List<AccountDto>> GetAllStaffAsync() =>
            (await _accountRepo.GetAllStaffAsync()).Select(Map).ToList();

        public async Task<List<AccountDto>> GetByRoleIdAsync(int roleId) =>
            (await _accountRepo.GetByRoleIdAsync(roleId)).Select(Map).ToList();

        // ===== GETs (Admin – no filter) =====
        public async Task<List<AccountDto>> GetAllStaffForAdminAsync() =>
            (await _accountRepo.AdminGetAllStaffAsync()).Select(Map).ToList();

        public async Task<List<AccountDto>> GetAllCustomerForAdminAsync() =>
            (await _accountRepo.AdminGetAllCustomerAsync()).Select(Map).ToList();

        public async Task<AccountDto?> GetByEmailAsync(string email) =>
            (await _accountRepo.GetByEmailAsync(email)) is { } e ? Map(e) : null;

        public async Task<AccountDto?> GetByIdAsync(int id) =>
            (await _accountRepo.GetByIdAsync(id)) is { } e ? Map(e) : null;

        // ===== CREATE / UPDATE =====
        public async Task<int> CreateAsync(AccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password không được để trống.");

            var entity = new Account
            {
                RoleId = dto.RoleId,
                AccountName = dto.AccountName,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Image = dto.Image,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsDeleted = dto.IsDeleted,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Provider = string.IsNullOrWhiteSpace(dto.Provider) ? "Local" : dto.Provider
            };

            await _accountRepo.AddAsync(entity);
            return entity.AccountId;
        }

        public async Task<bool> UpdateAsync(int id, AccountDto dto)
        {
            var entity = await _accountRepo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.RoleId = dto.RoleId;
            entity.AccountName = dto.AccountName;
            entity.PhoneNumber = dto.PhoneNumber;
            entity.Email = dto.Email;
            entity.Image = dto.Image;
            entity.IsDeleted = dto.IsDeleted;
            entity.Status = dto.Status;
            entity.UpdatedAt = DateTime.Now;
            entity.Provider = dto.Provider ?? entity.Provider;

            // Update password nếu client gửi plaintext (không phải hash)
            if (!string.IsNullOrWhiteSpace(dto.Password) && !IsBcryptHash(dto.Password))
                entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _accountRepo.UpdateAsync(entity);
            return true;
        }

        // ===== AUTH / VALIDATION =====
        public Task<bool> ExistsByEmailAsync(string email) => _accountRepo.ExistsByEmailAsync(email);
        public Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, int? excludeAccountId = null) =>
            _accountRepo.ExistsByPhoneAsync(phoneNumber, excludeAccountId);

        public async Task<AccountDto?> AuthenticateAsync(string email, string password)
        {
            var acc = await _accountRepo.GetByEmailAsync(email);
            if (acc != null && BCrypt.Net.BCrypt.Verify(password, acc.Password))
                return new AccountDto { Id = acc.AccountId, Email = acc.Email, RoleId = acc.RoleId, AccountName = acc.AccountName, Image = acc.Image };
            return null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Mật khẩu mới không được để trống.");

            var acc = await _accountRepo.GetByEmailAsync(email);
            if (acc == null) return false;

            acc.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            acc.UpdatedAt = DateTime.Now;

            await _accountRepo.UpdateAsync(acc);
            return true;
        }

        // ===== Helpers =====
        private static bool IsBcryptHash(string s) =>
            !string.IsNullOrWhiteSpace(s) && s.Length == 60 && (s.StartsWith("$2a$") || s.StartsWith("$2b$") || s.StartsWith("$2y$"));

        private static AccountDto Map(Account x) => new()
        {
            Id = x.AccountId,
            RoleId = x.RoleId,
            AccountName = x.AccountName,
            PhoneNumber = x.PhoneNumber,
            Email = x.Email,
            Image = x.Image,
            Password = x.Password,
            IsDeleted = x.IsDeleted,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Provider = string.IsNullOrWhiteSpace(x.Provider) ? "Local" : x.Provider
        };
    }
}
