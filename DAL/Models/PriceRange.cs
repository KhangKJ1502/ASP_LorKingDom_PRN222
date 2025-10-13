using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class PriceRange
{
    public int PriceRangeId { get; set; }

    // Giá tối thiểu
    public decimal PriceRangeMin { get; set; }

    // Giá tối đa
    public decimal PriceRangeMax { get; set; }

    // Trạng thái xóa mềm
    public bool IsDeleted { get; set; }

    // Ngày tạo
    public DateTime CreatedAt { get; set; }

    // Quan hệ với Product
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
