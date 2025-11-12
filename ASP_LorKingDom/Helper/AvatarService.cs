using BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using BLL.Validators;

namespace BLL.Services
{
    /// <summary>
    /// Service xử lý upload/delete avatar cho nhân viên
    /// Tách logic file handling ra khỏi Controller
    /// </summary>

    public class AvatarService : IAvatarService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedTypes = { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif" };
        private const long MaxFileSize = 2 * 1024 * 1024; // 2MB
        private const string UploadFolder = "uploads/staff";

        public AvatarService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Validate avatar file (type + size)
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateAvatarFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "File không hợp lệ.");

            // Dùng AccountValidator từ BLL
            var validation = AccountValidator.ValidateImageFile(file.Length, file.ContentType);

            if (!validation.IsValid)
                return (false, string.Join("; ", validation.Errors));

            return (true, null);
        }

        /// <summary>
        /// Lưu avatar file và trả về path hoặc error
        /// </summary>
        public async Task<(bool Success, string MessageOrPath)> SaveAvatarAsync(IFormFile file)
        {
            // Validate trước
            var (isValid, errorMsg) = ValidateAvatarFile(file);
            if (!isValid)
                return (false, errorMsg!);

            try
            {
                // Tạo folder nếu chưa tồn tại
                var uploadsPath = Path.Combine(_env.WebRootPath, UploadFolder);
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(uploadsPath, fileName);

                // Save file
                await using var stream = System.IO.File.Create(fullPath);
                await file.CopyToAsync(stream);

                // Return web path
                var webPath = $"/{UploadFolder}/{fileName}";
                return (true, webPath);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi lưu file: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa avatar cũ (nếu có)
        /// </summary>
        public void DeleteAvatar(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            try
            {
                var fileName = Path.GetFileName(imagePath);

                // Security: Chặn path traversal
                if (fileName.Contains("..") || Path.IsPathRooted(fileName))
                    return;

                var fullPath = Path.Combine(_env.WebRootPath, UploadFolder, fileName);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // Ignore errors when deleting old files
            }
        }
    }
}
