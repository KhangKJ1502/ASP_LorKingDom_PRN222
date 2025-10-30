using System.Text.RegularExpressions;

namespace BLL.Validators
{
    public static class AuthValidator
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static void ValidateLogin(string email, string password)
        {
            //if (string.IsNullOrWhiteSpace(email))
            //    throw new ArgumentException("Email không được để trống.");

            //if (!EmailRegex.IsMatch(email))
            //    throw new ArgumentException("Email không hợp lệ.");

            //if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            //    throw new ArgumentException("Mật khẩu phải ít nhất 6 ký tự.");
        }
    }
}
