using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public int SuperCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual SuperCategory SuperCategory { get; set; } = null!;
}
