using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;


namespace BLL.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;

        public AccountService(IAccountRepository accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public async Task<AccountDto?> GetByEmailAsync(string email)
        {
            var entity = await _accountRepo.GetByEmailAsync(email);
            return entity == null ? null : Map(entity);
        }

        public async Task<AccountDto?> GetByIdAsync(int id)
        {
            var entity = await _accountRepo.GetByIdAsync(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<int> CreateAsync(AccountDto dto)
        {
            var entity = new Account
            {
                RoleId = dto.RoleId,
                AccountName = dto.AccountName,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Image = dto.Image,
                Password = dto.Password,
                IsDeleted = dto.IsDeleted,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                Provider = dto.Provider
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
            entity.Password = dto.Password;
            entity.IsDeleted = dto.IsDeleted;
            entity.Status = dto.Status;
            entity.UpdatedAt = dto.UpdatedAt;
            entity.Provider = dto.Provider;

            await _accountRepo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _accountRepo.ExistsByEmailAsync(email);
        }

        public async Task<AccountDto?> AuthenticateAsync(string email, string password)
        {
            var account = await _accountRepo.GetByEmailAsync(email);
            if (account != null && BCrypt.Net.BCrypt.Verify(password, account.Password))
            {
                return new AccountDto { Id = account.AccountId, Email = account.Email, RoleId = account.RoleId };
            }
            return null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var account = await _accountRepo.GetByEmailAsync(email);
            if (account == null) return false;

            account.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.UpdatedAt = DateTime.Now;
            await _accountRepo.UpdateAsync(account);
            return true;
        }

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
            Provider = x.Provider
        };
    }
}
