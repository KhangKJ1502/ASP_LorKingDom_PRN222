namespace BLL.DTOs
{
    /// <summary>
    /// Request model cho việc tạo/cập nhật nhân viên
    /// Dùng để group các field form thay vì nhiều tham số riêng lẻ
    /// </summary>
    public class StaffFormModel
    {
        // ===== Thông Tin Cơ Bản =====
        public int Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? RoleId { get; set; }

        // ===== Mật Khẩu =====
        /// <summary>Password cho Create, NewPassword cho Update</summary>
        public string? Password { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }

        // ===== Trạng Thái =====
        public string Status { get; set; } = "Active";
        public bool IsDeleted { get; set; }

        // ===== Avatar (Controller sẽ xử lý file, chỉ lưu path) =====
        public string? Image { get; set; }
        public string? ExistingImage { get; set; }
        public bool RemoveImage { get; set; }

        // ===== Navigation Context =====
        public string? SearchQuery { get; set; }
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Chuyển sang AccountDto để gọi Service
        /// </summary>
        public AccountDto ToAccountDto()
        {
            return new AccountDto
            {
                Id = this.Id,
                AccountName = this.AccountName?.Trim() ?? string.Empty,
                Email = this.Email?.Trim().ToLower() ?? string.Empty,
                PhoneNumber = string.IsNullOrWhiteSpace(this.PhoneNumber) ? null : this.PhoneNumber.Trim(),
                RoleId = this.RoleId,
                Password = this.Id == 0 ? this.Password : this.NewPassword, // Create vs Update
                Status = this.Status,
                IsDeleted = this.IsDeleted,
                Image = this.Image ?? this.ExistingImage // Ưu tiên Image mới, fallback sang ExistingImage
            };
        }

        /// <summary>
        /// Lấy password tương ứng với mode (Create/Update)
        /// </summary>
        public string? GetPassword() => this.Id == 0 ? this.Password : this.NewPassword;

        /// <summary>
        /// Lấy confirm password tương ứng với mode
        /// </summary>
        public string? GetConfirmPassword() => this.Id == 0 ? this.ConfirmPassword : this.ConfirmNewPassword;
    }
}
