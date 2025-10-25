using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? Sku { get; set; }                      // sinh trong Service khi tạo mới
        public string ProductName { get; set; } = "";

        // FK (nullable theo DB)
        public int? CategoryId { get; set; }
        public int? MaterialId { get; set; }
        public int? AgeId { get; set; }
        public int? SexId { get; set; }
        public int? PriceRangeId { get; set; }
        public int? BrandId { get; set; }
        public int? OriginId { get; set; }

        public decimal Price { get; set; }
        public int StockQuantity { get; set; }                // map -> Quantity
        public string ProductStatus { get; set; } = "Available";

        public string? DescriptionHtml { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Hiển thị (optional)
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? SexName { get; set; }
        public string? MaterialName { get; set; }
        public string? AgeRange { get; set; }
        public string? OriginName { get; set; }
        public string? PriceRangeName { get; set; }           // 💡 thêm để hiển thị mức giá (0–500k, 500k–1tr,...)

        // Ảnh
        public string? MainImageUrl { get; set; }             // IsMain = true
        public List<string> SecondaryImageUrls { get; set; } = new();

        // 💡 bổ sung tiện ích hiển thị
        public bool IsOutOfStock => StockQuantity <= 0;       // tiện cho view check hết hàng
        public bool IsOnSale { get; set; }                    // có thể set true nếu có khuyến mãi

        public bool IsLiked { get; set; }
    }

}
