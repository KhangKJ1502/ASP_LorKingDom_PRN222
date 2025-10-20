// BLL/DTOs/PromotionDto.cs
using System;
using System.Collections.Generic;

namespace BLL.DTOs
{
    public class PromotionDto
    {
        public int PromotionId { get; set; }
        public string PromotionCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }  // inclusive
        public DateTime EndDate { get; set; }    // inclusive
        public string Status { get; set; } = "Inactive"; // "Active"/"Inactive"
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PromotionCreateDto
    {
        public string PromotionCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }    // inclusive
        public DateTime EndDate { get; set; }      // inclusive
        public string Status { get; set; } = "Inactive"; // mặc định khi tạo mới
    }

    public class PromotionUpdateDto
    {
        public int PromotionId { get; set; }
        public string PromotionCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Inactive";
    }

    /// <summary>DTO gom field từ form modal (Create/Update chung 1 form).</summary>
    public class PromotionSaveDto
    {
        public int PromotionId { get; set; }
        public string PromotionCode { get; set; } = "";
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";
    }
}