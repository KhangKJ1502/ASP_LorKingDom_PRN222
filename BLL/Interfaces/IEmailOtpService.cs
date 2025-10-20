namespace BLL.Interfaces
{
    public interface IEmailOtpService
    {
        Task SendOtpAsync(string email, string purpose = "Register");
        Task<bool> VerifyOtpAsync(string email, string otpCode, string purpose = "Register");
        Task<bool> CanResendOtpAsync(string email, string purpose = "Register");
        Task SendWelcomeEmailAsync(string email, string accountName);
        Task SendPasswordResetEmailAsync(string email, string newPassword);
    }
}
