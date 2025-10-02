using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Age
{
    public int AgeId { get; set; }

    public string AgeRange { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
