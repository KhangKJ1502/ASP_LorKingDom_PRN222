using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using MailKit.Net.Smtp;
using MimeKit;


namespace BLL.Services
{
    public class EmailOtpService : IEmailOtpService
    {
        private readonly IEmailOtpRepository _emailOtpRepo;

        // Cấu hình SMTP
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string FromEmail = "vuquangduc1404@gmail.com";
        private const string FromName = "LordKingDom";
        private const string AppPassword = "bmexrjlskuwzqesp"; // 🔒 App Password Gmail

        public EmailOtpService(IEmailOtpRepository emailOtpRepo)
        {
            _emailOtpRepo = emailOtpRepo;
        }

        /// <summary>
        /// Gửi mã OTP đến email người dùng và lưu vào database.
        /// </summary>
        public async Task SendOtpAsync(string email, string purpose = "Register")
        {
            // Kiểm tra nếu đã có OTP active
            if (await _emailOtpRepo.ExistsActiveOtpAsync(email, purpose))
                throw new InvalidOperationException("OTP đã được gửi. Vui lòng chờ hoặc gửi lại sau.");

            // Tạo OTP ngẫu nhiên (6 chữ số)
            var otpCode = new Random().Next(100000, 999999).ToString();

            var otpEntity = new EmailOtp
            {
                Email = email,
                Purpose = purpose,
                OtpCode = otpCode,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(5),
                IsUsed = false
            };

            await _emailOtpRepo.AddAsync(otpEntity);

            // Nội dung email OTP
            string subject = "Mã OTP xác thực tài khoản của bạn";
            string body = $@"
                <html>
                <body style='font-family:Segoe UI,Roboto,sans-serif;background:#fff7f2;padding:24px;'>
                    <div style='max-width:600px;margin:auto;background:white;border-radius:16px;
                                padding:28px;box-shadow:0 8px 24px rgba(249,115,22,0.2);'>
                        <h2 style='color:#f97316;text-align:center;'>🔐 Mã xác thực Lordkingdom</h2>
                        <p style='font-size:16px;color:#333;text-align:center;'>
                            Xin chào!<br/>Mã OTP của bạn là:
                        </p>
                        <div style='text-align:center;margin:24px 0;'>
                            <span style='font-size:28px;font-weight:700;
                                         background:linear-gradient(135deg,#f97316,#fb923c);
                                         -webkit-background-clip:text;-webkit-text-fill-color:transparent;'>
                                {otpCode}
                            </span>
                        </div>
                        <p style='color:#666;font-size:14px;text-align:center;'>
                            Mã có hiệu lực trong 5 phút.<br/>Vui lòng không chia sẻ mã này với bất kỳ ai.
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, isHtml: true);
        }

        /// <summary>
        /// Xác minh mã OTP và đánh dấu đã sử dụng.
        /// </summary>
        public async Task<bool> VerifyOtpAsync(string email, string otpCode, string purpose = "Register")
        {
            var otp = await _emailOtpRepo.GetByEmailAndPurposeAsync(email, purpose);
            if (otp == null || otp.OtpCode != otpCode)
                return false;

            otp.IsUsed = true;
            await _emailOtpRepo.UpdateAsync(otp);
            return true;
        }

        /// <summary>
        /// Kiểm tra xem có thể gửi lại OTP hay không.
        /// </summary>
        public async Task<bool> CanResendOtpAsync(string email, string purpose = "Register")
        {
            return !await _emailOtpRepo.ExistsActiveOtpAsync(email, purpose);
        }

        /// <summary>
        /// 🆕 Gửi thư chào mừng người dùng sau khi đăng ký thành công.
        /// </summary>
        public async Task SendWelcomeEmailAsync(string email, string accountName)
        {
            string subject = "🎉 Chào mừng bạn đến với Lordkingdom!";
            string body = $@"
                <html>
                <body style='font-family:Segoe UI,Roboto,sans-serif;background:#fff7f2;padding:24px;'>
                    <div style='max-width:600px;margin:auto;background:white;border-radius:16px;
                                padding:30px;box-shadow:0 8px 24px rgba(255,122,42,0.25);text-align:center;'>
                        <div style='font-size:42px;margin-bottom:8px;'>👑</div>
                        <h2 style='color:#f97316;margin-bottom:10px;'>Chào mừng, {accountName}!</h2>
                        <p style='color:#555;font-size:15px;line-height:1.6;'>
                            Cảm ơn bạn đã gia nhập cộng đồng <strong>Lordkingdom</strong>.<br/>
                            Hãy bắt đầu hành trình cùng chúng tôi để lan tỏa năng lượng tích cực!
                        </p>
                        <a href='https://lordkingdom.com' target='_blank'
                           style='display:inline-block;margin-top:16px;
                                  background:linear-gradient(135deg,#f97316,#fb923c);
                                  color:white;padding:12px 28px;border-radius:10px;
                                  text-decoration:none;font-weight:600;'>
                            Khám phá ngay
                        </a>
                        <hr style='margin:28px 0;border:none;border-top:1px solid #ffe3d0;'/>
                        <small style='color:#aaa;'>© 2025 Lordkingdom. All rights reserved.</small>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, isHtml: true);
        }

        public async Task SendPasswordResetEmailAsync(string email, string newPassword)
        {
            var subject = "Đặt lại mật khẩu - ToyStore";
            var body = $@"
                <html>
                <body style='font-family:Segoe UI,Roboto,sans-serif;background:#fff7f2;padding:24px;'>
                    <div style='max-width:600px;margin:auto;background:white;border-radius:16px;
                                padding:28px;box-shadow:0 8px 24px rgba(249,115,22,0.2);'>
                        <h2 style='color:#f97316;text-align:center;'>🔑 Đặt lại mật khẩu</h2>
                        <p style='font-size:16px;color:#333;text-align:center;'>
                            Xin chào!<br/>Mật khẩu mới của bạn là:
                        </p>
                        <div style='text-align:center;margin:24px 0;'>
                            <span style='font-size:28px;font-weight:700;
                                         background:linear-gradient(135deg,#f97316,#fb923c);
                                         -webkit-background-clip:text;-webkit-text-fill-color:transparent;'>
                                {newPassword}
                            </span>
                        </div>
                        <p style='color:#666;font-size:14px;text-align:center;'>
                            Vui lòng đăng nhập và thay đổi mật khẩu ngay lập tức.<br/>
                            Nếu bạn không yêu cầu, hãy bỏ qua email này.
                        </p>
                    </div>
                </body>
                </html>";
            await SendEmailAsync(email, subject, body, isHtml: true); // Giả sử bạn có method SendEmailAsync
        }

        /// <summary>
        /// Hàm dùng chung để gửi email qua SMTP Gmail.
        /// Có thể gửi dạng text hoặc HTML.
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(SmtpServer, SmtpPort, false);
                await client.AuthenticateAsync(FromEmail, AppPassword);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
