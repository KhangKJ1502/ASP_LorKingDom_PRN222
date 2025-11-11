using System.Text.RegularExpressions;
using BLL.DTOs;

namespace BLL.Validators
{
    public static class ProductValidator
    {
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled);
        private static readonly Regex NameRegex =
            new(@"^[\p{L}\p{N}\s\-\&\/\+\.\(\)]+$", RegexOptions.Compiled);
        private static bool IsNullOrWhiteSpaceHtml(string? html)
        {
            var s = (html ?? "").Trim();
            if (s.Length == 0) return true;

            // bỏ thẻ, thay &nbsp; và trim lại
            s = HtmlTagRegex.Replace(s, "");
            s = s.Replace("&nbsp;", " ").Trim();

            return string.IsNullOrWhiteSpace(s);
        }
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

            if (IsNullOrWhiteSpaceHtml(dto.DescriptionHtml))
                throw new ArgumentException("Mô tả không được để trống.");
            if ((dto.DescriptionHtml ?? "").Length > 20000)
                throw new ArgumentException("Mô tả quá dài (tối đa 20.000 ký tự).");

            if (dto.Price < 0) throw new ArgumentException("Giá không hợp lệ.");
            if (dto.Price > 99999999.99m) throw new ArgumentException("Giá không được vượt quá 99,999,999.99 VNĐ.");

            if (dto.StockQuantity < 0) throw new ArgumentException("Số lượng không hợp lệ.");
            if (dto.StockQuantity >= 99999) throw new ArgumentException("Số lượng không vượt quá 99.999.");

            // điểm mấu chốt: chuẩn hoá rồi mới kiểm tra
            var status = NormalizeStatus(dto.ProductStatus);
            dto.ProductStatus = status; // ghi lại để các lớp sau dùng
            if (status is not ("Available" or "OutOfStock" or "Discontinued"))
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
            // Chuẩn hoá để null/"" -> "Available"
            dto.ProductStatus = NormalizeStatus(dto.ProductStatus);

            Validate(dto); // giờ thì hợp lệ

            if (dto.CategoryId == null) throw new ArgumentException("Vui lòng chọn Danh mục.");
            if (dto.BrandId == null) throw new ArgumentException("Vui lòng chọn Thương hiệu.");
            if (dto.SexId == null) throw new ArgumentException("Vui lòng chọn Giới tính.");
            if (dto.AgeId == null) throw new ArgumentException("Vui lòng chọn Khoảng tuổi.");
            if (dto.MaterialId == null) throw new ArgumentException("Vui lòng chọn Chất liệu.");
            if (dto.OriginId == null) throw new ArgumentException("Vui lòng chọn Nguồn gốc.");
            if (dto.PriceRangeId == null) throw new ArgumentException("Vui lòng chọn Khoảng giá.");

            var secCount = dto.SecondaryImageUrls?.Count(u => !string.IsNullOrWhiteSpace(u)) ?? 0;
            if (secCount < 4) throw new ArgumentException("Vui lòng chọn ít nhất 4 ảnh chi tiết.");
            if (secCount > 6) throw new ArgumentException("Tối đa 6 ảnh chi tiết.");
        }

       public static void ValidateForUpdate(ProductDto dto)
{
    // 1) Chuẩn hoá status để tránh null/"" làm fail
    dto.ProductStatus = NormalizeStatus(dto.ProductStatus);

    // 2) Validate chung
    Validate(dto);

    // 3) Validate các field bắt buộc
    if (dto.CategoryId == null) throw new ArgumentException("Vui lòng chọn Danh mục.");
    if (dto.BrandId == null) throw new ArgumentException("Vui lòng chọn Thương hiệu.");
    if (dto.SexId == null) throw new ArgumentException("Vui lòng chọn Giới tính.");
    if (dto.AgeId == null) throw new ArgumentException("Vui lòng chọn Khoảng tuổi.");
    if (dto.MaterialId == null) throw new ArgumentException("Vui lòng chọn Chất liệu.");
    if (dto.OriginId == null) throw new ArgumentException("Vui lòng chọn Nguồn gốc.");
    if (dto.PriceRangeId == null) throw new ArgumentException("Vui lòng chọn Khoảng giá.");

    // 4) Luật ảnh (cứng 4–6)
    var secCount = dto.SecondaryImageUrls?.Count(u => !string.IsNullOrWhiteSpace(u)) ?? 0;
    if (secCount < 4) throw new ArgumentException("Vui lòng chọn ít nhất 4 ảnh chi tiết.");
    if (secCount > 6) throw new ArgumentException("Tối đa 6 ảnh chi tiết.");
}

    }
}
