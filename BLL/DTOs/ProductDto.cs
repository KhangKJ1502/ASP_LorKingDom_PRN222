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
        public string? Sku { get; set; }                    
        public string ProductName { get; set; } = "";

        public int? CategoryId { get; set; }
        public int? MaterialId { get; set; }
        public int? AgeId { get; set; }
        public int? SexId { get; set; }
        public int? PriceRangeId { get; set; }
        public int? BrandId { get; set; }
        public int? OriginId { get; set; }

        public decimal Price { get; set; }
        public int StockQuantity { get; set; }            
        public string ProductStatus { get; set; } = "Available";

        public string? DescriptionHtml { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? SexName { get; set; }
        public string? MaterialName { get; set; }
        public string? AgeRange { get; set; }
        public string? OriginName { get; set; }
        public string? PriceRangeName { get; set; }          
        public string? MainImageUrl { get; set; }         
        public List<string> SecondaryImageUrls { get; set; } = new();

        public bool IsOutOfStock => StockQuantity <= 0;       
    public bool IsOnSale { get; set; }                   

    public int? PromotionId { get; set; }
    public string? PromotionCode { get; set; }
    public decimal? PromotionDiscountPercent { get; set; }

        public bool IsLiked { get; set; }
    }

}
