using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class PriceRange
{
    public int PriceRangeId { get; set; }

    public decimal PriceRangeMin { get; set; }

    public decimal PriceRangeMax { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
