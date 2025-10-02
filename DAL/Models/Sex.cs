using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Sex
{
    public int SexId { get; set; }

    public string SexName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
