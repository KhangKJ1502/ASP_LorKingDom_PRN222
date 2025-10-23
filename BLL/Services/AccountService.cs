using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;
using System.Security.Claims;

namespace BLL.Services
{

    public static class AccountConstants
    {
        public const string StatusActive = "Active";
        public const string StatusInactive = "Inactive";
        public const string ProviderLocal = "Local";

        public static readonly string[] AllowedStatuses = [StatusActive, StatusInactive];
    }

    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IRoleRepository _roleRepo;

        public AccountService(IAccountRepository accountRepo, IRoleRepository roleRepo)
        {
            _accountRepo = accountRepo;
            _roleRepo = roleRepo;
        }

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

        public async Task<AccountDto?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.");

            var normEmail = NormalizeEmail(email);
            return (await _accountRepo.GetByEmailAsync(normEmail)) is { } e ? Map(e) : null;
        }

        public async Task<AccountDto?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID không hợp lệ.");

            return (await _accountRepo.GetByIdAsync(id)) is { } e ? Map(e) : null;
        }

        // ===== CREATE =====
        public async Task<int> CreateAsync(AccountDto dto)
        {
            // 1) Validate format
            var validation = AccountValidator.ValidateCreateStaff(
                dto.AccountName, dto.Email, dto.PhoneNumber, dto.RoleId, dto.Password, dto.Password);

            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join("; ", validation.Errors));

            // 2) Validate nghiệp vụ
            await ValidateRoleExists(dto.RoleId);
            var normEmail = NormalizeEmail(dto.Email);
            await ValidateEmailUnique(normEmail);
            var normPhone = NormalizePhone(dto.PhoneNumber);
            await ValidatePhoneUnique(normPhone);

            // 3) Map entity
            var entity = new Account
            {
                RoleId = dto.RoleId,

                AccountName = dto.AccountName,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email.Trim().ToLower(),
                Image = dto.Image,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim()),
                IsDeleted = dto.IsDeleted,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Provider = AccountConstants.ProviderLocal
            };

            await _accountRepo.AddAsync(entity);
            return entity.AccountId;
        }

        // ===== UPDATE =====
        public async Task<bool> UpdateAsync(int id, AccountDto dto)
        {
            if (id <= 0) throw new ArgumentException("ID không hợp lệ.");

            // 1) Validate format (password optional)
            var validation = AccountValidator.ValidateUpdateStaff(
                dto.AccountName, dto.Email, dto.PhoneNumber, dto.RoleId, dto.Status, dto.Password, dto.Password);

            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join("; ", validation.Errors));

            // 2) Lấy entity
            var entity = await _accountRepo.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản với ID {id}.");

            // 3) Validate nghiệp vụ
            await ValidateRoleExists(dto.RoleId);

            // Không cho đổi email
            var normDtoEmail = NormalizeEmail(dto.Email);
            if (!string.Equals(entity.Email, normDtoEmail, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Không được phép thay đổi email.");

            // Phone unique (trừ chính nó)
            var normPhone = NormalizePhone(dto.PhoneNumber);
            await ValidatePhoneUnique(normPhone, id);

            // 4) Cập nhật
            entity.RoleId = dto.RoleId;
            entity.AccountName = dto.AccountName?.Trim();
            entity.PhoneNumber = normPhone;
            entity.Image = dto.Image;
            entity.IsDeleted = dto.IsDeleted;
            entity.Status = dto.IsDeleted ? AccountConstants.StatusInactive : NormalizeStatus(dto.Status);
            entity.UpdatedAt = DateTime.Now;

            // 5) Update password nếu có (không hash nếu đã là bcrypt)
            if (!string.IsNullOrWhiteSpace(dto.Password) && !IsBcryptHash(dto.Password))
            {
                entity.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _accountRepo.UpdateAsync(entity);
            return true;
        }

        // ===== SOFT DELETE / RESTORE =====
        public async Task<bool> SoftDeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID không hợp lệ.");

            var entity = await _accountRepo.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản với ID {id}.");

            if (entity.IsDeleted) throw new InvalidOperationException("Tài khoản đã bị xóa trước đó.");

            entity.IsDeleted = true;
            entity.Status = AccountConstants.StatusInactive;
            entity.UpdatedAt = DateTime.Now;

            await _accountRepo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID không hợp lệ.");

            var entity = await _accountRepo.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản với ID {id}.");

            if (!entity.IsDeleted) throw new InvalidOperationException("Tài khoản chưa bị xóa.");

            entity.IsDeleted = false;
            entity.Status = AccountConstants.StatusActive;
            entity.UpdatedAt = DateTime.Now;

            await _accountRepo.UpdateAsync(entity);
            return true;
        }

        // ===== AUTH / EXISTS =====
        public Task<bool> ExistsByEmailAsync(string email) =>
            _accountRepo.ExistsByEmailAsync(NormalizeEmail(email));

        public Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, int? excludeAccountId = null) =>
            _accountRepo.ExistsByPhoneAsync(NormalizePhone(phoneNumber), excludeAccountId);

        public async Task<AccountDto?> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password không được để trống.");

            var acc = await _accountRepo.GetByEmailAsync(NormalizeEmail(email));
            if (acc == null) return null;

            if (acc.IsDeleted) throw new InvalidOperationException("Tài khoản đã bị khóa.");
            if (!string.Equals(acc.Status, AccountConstants.StatusActive, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tài khoản chưa được kích hoạt.");

            if (!BCrypt.Net.BCrypt.Verify(password, acc.Password)) return null;

            return Map(acc);
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.");

            var validation = AccountValidator.ValidatePassword(newPassword);
            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join("; ", validation.Errors));

            var acc = await _accountRepo.GetByEmailAsync(NormalizeEmail(email))
                      ?? throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

            acc.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            acc.UpdatedAt = DateTime.Now;

            await _accountRepo.UpdateAsync(acc);
            return true;
        }

        // ===== PRIVATE VALIDATORS / NORMALIZERS =====
        private async Task ValidateRoleExists(int? roleId)
        {
            if (!roleId.HasValue || roleId.Value <= 0)
                throw new InvalidOperationException("RoleId không hợp lệ.");

            var role = await _roleRepo.GetByIdAsync(roleId.Value);
            if (role == null)
                throw new KeyNotFoundException($"Không tìm thấy vai trò với ID {roleId.Value}.");
        }

        private async Task ValidateEmailUnique(string normalizedEmail)
        {
            if (string.IsNullOrWhiteSpace(normalizedEmail)) return;
            if (await _accountRepo.ExistsByEmailAsync(normalizedEmail))
                throw new InvalidOperationException($"Email '{normalizedEmail}' đã tồn tại trong hệ thống.");
        }

        private async Task ValidatePhoneUnique(string normalizedPhone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(normalizedPhone)) return; // optional
            if (await _accountRepo.ExistsByPhoneAsync(normalizedPhone, excludeId))
                throw new InvalidOperationException($"Số điện thoại '{normalizedPhone}' đã tồn tại trong hệ thống.");
        }

        private static string NormalizeEmail(string email) =>
            string.IsNullOrWhiteSpace(email) ? email : email.Trim().ToLowerInvariant();

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            var trimmed = phone.Trim();
            // remove whitespaces/dashes/brackets
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"[\s\-\(\)]+", "");
            if (trimmed.StartsWith("+84")) trimmed = "0" + trimmed[3..];
            return trimmed;
        }

        private static string NormalizeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return AccountConstants.StatusActive;
            var s = status.Trim();
            return AccountConstants.AllowedStatuses.Any(x => x.Equals(s, StringComparison.OrdinalIgnoreCase))
                ? AccountConstants.AllowedStatuses.First(x => x.Equals(s, StringComparison.OrdinalIgnoreCase))
                : AccountConstants.StatusActive;
        }

        public async Task<AccountDto?> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            if (user == null) return null;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            return await GetByIdAsync(userId);
        }

        // ===== Helpers =====

        private static bool IsBcryptHash(string s) =>
            !string.IsNullOrWhiteSpace(s) && s.Length == 60 &&
            (s.StartsWith("$2a$") || s.StartsWith("$2b$") || s.StartsWith("$2y$"));

        private static AccountDto Map(Account x) => new()
        {
            Id = x.AccountId,
            RoleId = x.RoleId,
            AccountName = x.AccountName,
            PhoneNumber = x.PhoneNumber,
            Email = x.Email,
            Image = x.Image,
            Password = x.Password, // chú ý: không trả về ra API public
            IsDeleted = x.IsDeleted,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Provider = x.Provider ?? AccountConstants.ProviderLocal
        };
    }
}
