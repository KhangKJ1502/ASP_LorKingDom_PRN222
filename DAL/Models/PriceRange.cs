using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class PriceRange
{
    public int PriceRangeId { get; set; }

    public string PriceRange1 { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
