using System;

namespace BLL.DTOs
{
    public sealed class PriceRangeDto
    {
        public int Id { get; set; }
        public decimal PriceRangeMin { get; set; }
        public decimal PriceRangeMax { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Hiển thị thân thiện
        public string DisplayRange => $"{PriceRangeMin:N0} - {PriceRangeMax:N0}";
    }
}
