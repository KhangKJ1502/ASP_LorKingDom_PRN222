using System.Text.RegularExpressions;

namespace BLL.Validators
{
    public class AccountValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new();
            public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

            public void AddError(string error)
            {
                if (!string.IsNullOrWhiteSpace(error)) Errors.Add(error);
                IsValid = false;
            }
        }

        // ===== Field Validators =====
        public static ValidationResult ValidateAccountName(string accountName)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(accountName))
            {
                result.AddError("Tên nhân viên bắt buộc.");
                return result;
            }

            var name = accountName.Trim();
            if (name.Length < 2) result.AddError("Tên nhân viên phải có ít nhất 2 ký tự.");
            if (name.Length > 100) result.AddError("Tên nhân viên không được vượt quá 100 ký tự.");

            if (!Regex.IsMatch(name, @"^[a-zA-Z0-9À-ỿ\s\-\.]+$"))
                result.AddError("Tên nhân viên chỉ chứa chữ, số, khoảng trắng và dấu '-', '.'");
            return result;
        }

        public static ValidationResult ValidateEmail(string email)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(email))
            {
                result.AddError("Email bắt buộc.");
                return result;
            }

            var e = email.Trim();
            if (e.Length > 255) result.AddError("Email không được vượt quá 255 ký tự.");
            if (!Regex.IsMatch(e, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                result.AddError("Email không hợp lệ.");
            return result;
        }

        // VN phone: optional; accept 0xxxxxxxxx or +84xxxxxxxxx (normalize ở service/controller)
        public static ValidationResult ValidatePhoneNumber(string phoneNumber)
        {
            var result = new ValidationResult();
            
            // Allow empty/null phone numbers
            if (string.IsNullOrWhiteSpace(phoneNumber)) 
                return result;

            try
            {
                var cleaned = Regex.Replace(phoneNumber.Trim(), @"[\s\-\(\)\.]+", "");
                
                // Handle +84 prefix
                if (cleaned.StartsWith("+84"))
                {
                    if (cleaned.Length < 12) // +84 + 9 digits minimum
                    {
                        result.AddError("Số điện thoại với +84 phải có đủ 9 số sau mã quốc gia.");
                        return result;
                    }
                    cleaned = "0" + cleaned.Substring(3);
                }
                else if (cleaned.StartsWith("84") && cleaned.Length >= 11)
                {
                    // Handle 84xxxxxxxxx without +
                    cleaned = "0" + cleaned.Substring(2);
                }
                
                // Check if it matches Vietnamese phone format (10 digits starting with 0)
                if (!Regex.IsMatch(cleaned, @"^0\d{9}$"))
                {
                    result.AddError("Số điện thoại phải có 10 số (bắt đầu từ 0) hoặc định dạng +84xxxxxxxxx.");
                    return result;
                }

                // Prefix check - relaxed validation
                if (cleaned.Length >= 3)
                {
                    var prefix = cleaned.Substring(0, 3);
                    var validPrefixes = new[]
                    {
                        "080","081","082","083","084","085","086","087","088","089",
                        "030","031","032","033","034","035","036","037","038","039",
                        "050","051","052","053","054","055","056","057","058","059",
                        "070","076","077","078","079",
                        "090","091","092","093","094","096","097","098","099"
                    };
                    
                    if (!Array.Exists(validPrefixes, p => p == prefix))
                    {
                        // Warning only, not blocking
                        result.AddError($"Đầu số {prefix} có thể không hợp lệ. Vui lòng kiểm tra lại.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Số điện thoại không hợp lệ: {ex.Message}");
            }

            return result;
        }

        public static ValidationResult ValidatePassword(string password)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(password))
            {
                result.AddError("Mật khẩu bắt buộc.");
                return result;
            }
            if (password.Length < 6) result.AddError("Mật khẩu phải có ít nhất 6 ký tự.");
            if (password.Length > 128) result.AddError("Mật khẩu không được vượt quá 128 ký tự.");
            return result;
        }

        public static ValidationResult ValidatePasswordMatch(string password, string confirmPassword)
        {
            var result = new ValidationResult();
            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                result.AddError("Xác nhận mật khẩu không khớp.");
            return result;
        }

        public static ValidationResult ValidateNewPassword(string newPassword, string confirmPassword)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(newPassword) && string.IsNullOrWhiteSpace(confirmPassword))
                return result;

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                result.AddError("Phải nhập cả mật khẩu mới và xác nhận.");
                return result;
            }

            if (newPassword.Length < 6) result.AddError("Mật khẩu mới phải có ít nhất 6 ký tự.");
            if (newPassword.Length > 128) result.AddError("Mật khẩu mới không được vượt quá 128 ký tự.");
            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                result.AddError("Xác nhận mật khẩu mới không khớp.");
            return result;
        }

        public static ValidationResult ValidateRoleId(int? roleId)
        {
            var result = new ValidationResult();
            if (!roleId.HasValue || roleId.Value <= 0)
                result.AddError("Vui lòng chọn vai trò (Role) hợp lệ.");
            return result;
        }

        public static ValidationResult ValidateStatus(string status)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(status))
            {
                result.AddError("Trạng thái bắt buộc.");
                return result;
            }
            var s = status.Trim();
            var valid = new[] { "Active", "Inactive" };
            if (!valid.Any(v => v.Equals(s, StringComparison.OrdinalIgnoreCase)))
                result.AddError("Trạng thái không hợp lệ. Chỉ hỗ trợ 'Active' hoặc 'Inactive'.");
            return result;
        }

        public static ValidationResult ValidateImageFile(long? fileSize, string contentType)
        {
            var result = new ValidationResult();
            if (!fileSize.HasValue || fileSize == 0) return result;

            const long maxSize = 2 * 1024 * 1024;
            if (fileSize > maxSize) result.AddError("Kích thước ảnh tối đa 2MB.");

            var allowed = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif" };
            if (!string.IsNullOrWhiteSpace(contentType) && !allowed.Any(t => t.Equals(contentType, StringComparison.OrdinalIgnoreCase)))
                result.AddError("Định dạng ảnh không hợp lệ. Hỗ trợ PNG/JPG/WebP/GIF.");
            return result;
        }

        // ===== Combined =====
        public static ValidationResult ValidateCreateStaff(
            string accountName, string email, string phoneNumber,
            int? roleId, string password, string confirmPassword)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateAccountName(accountName).Errors);
            result.Errors.AddRange(ValidateEmail(email).Errors);
            result.Errors.AddRange(ValidatePhoneNumber(phoneNumber).Errors);
            result.Errors.AddRange(ValidateRoleId(roleId).Errors);
            result.Errors.AddRange(ValidatePassword(password).Errors);
            result.Errors.AddRange(ValidatePasswordMatch(password, confirmPassword).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public static ValidationResult ValidateUpdateStaff(
            string accountName, string email, string phoneNumber,
            int? roleId, string status, string? newPassword = null, string? confirmNewPassword = null)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateAccountName(accountName).Errors);
            result.Errors.AddRange(ValidateEmail(email).Errors);
            result.Errors.AddRange(ValidatePhoneNumber(phoneNumber).Errors);
            result.Errors.AddRange(ValidateRoleId(roleId).Errors);
            result.Errors.AddRange(ValidateStatus(status).Errors);

            if (!string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmNewPassword))
                result.Errors.AddRange(ValidateNewPassword(newPassword ?? "", confirmNewPassword ?? "").Errors);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
}
