using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs
{
    public class SignupRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
        [DataType(DataType.Password)] // Giúp input hiển thị dạng password
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận Mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu và Mật khẩu xác nhận không khớp.")]
        public string? ConfirmPassword { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với Điều khoản dịch vụ và Chính sách bảo mật.")]
        public bool AgreeTerms { get; set; }
    }
}
