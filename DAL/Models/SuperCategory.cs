using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class SuperCategory
{
    public int SuperCategoryId { get; set; }

    public string SuperCategoryName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
