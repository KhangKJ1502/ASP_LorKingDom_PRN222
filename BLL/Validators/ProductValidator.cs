using System.Text.RegularExpressions;
using BLL.DTOs;

namespace BLL.Validators
{
    public static class ProductValidator
    {
        private static readonly Regex NameRegex =
            new(@"^[\p{L}\p{N}\s\-\&\/\+\.\(\)]+$", RegexOptions.Compiled);

        public static void Validate(ProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            dto.ProductName = (dto.ProductName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dto.ProductName))
                throw new ArgumentException("Tên sản phẩm không được để trống.");
            if (dto.ProductName.Length > 255)
                throw new ArgumentException("Tên sản phẩm không được vượt quá 255 ký tự.");
            if (!NameRegex.IsMatch(dto.ProductName))
                throw new ArgumentException("Tên chỉ được chứa chữ, số, khoảng trắng và - & / + . ( ).");
            if (dto.Price < 0)
                throw new ArgumentException("Giá không hợp lệ.");
            if (dto.Price > 99999999.99m)
                throw new ArgumentException("Giá không được vượt quá 99,999,999.99 VNĐ.");

            if (dto.StockQuantity < 0) throw new ArgumentException("Số lượng không hợp lệ.");

            // ✅ Validate bằng code tiếng Anh để khớp DB
            if (dto.ProductStatus is not ("Available" or "OutOfStock" or "Discontinued"))
                throw new ArgumentException("Trạng thái không hợp lệ.");
        }

        public static string NormalizeStatus(string? s)
        {
            return (s ?? "").Trim() switch
            {
                "Hoạt động" => "Available",
                "Hết hàng" => "OutOfStock",
                "Không còn hoạt động" => "Discontinued",
                _ => (string.IsNullOrWhiteSpace(s) ? "Available" : s!)
            };
        }
        public static void ValidateForCreate(ProductDto dto)
        {
            Validate(dto); // gọi validate chung
            if (dto.CategoryId == null) throw new ArgumentException("Vui lòng chọn Danh mục.");
            if (dto.BrandId == null) throw new ArgumentException("Vui lòng chọn Thương hiệu.");
            if (dto.SexId == null) throw new ArgumentException("Vui lòng chọn Giới tính.");
            if (dto.AgeId == null) throw new ArgumentException("Vui lòng chọn Khoảng tuổi.");
            if (dto.MaterialId == null) throw new ArgumentException("Vui lòng chọn Chất liệu.");
            if (dto.OriginId == null) throw new ArgumentException("Vui lòng chọn Nguồn gốc.");
            if (dto.PriceRangeId == null) throw new ArgumentException("Vui lòng chọn Khoảng giá.");
            var secCount = dto.SecondaryImageUrls?.Count(u => !string.IsNullOrWhiteSpace(u)) ?? 0;
            if (secCount < 4)
                throw new ArgumentException("Vui lòng chọn ít nhất 4 ảnh chi tiết.");
            if (secCount > 6)
                throw new ArgumentException("Tối đa 6 ảnh chi tiết.");

        }

    }
}
