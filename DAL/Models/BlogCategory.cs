using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class BlogCategory
{
    public int BlogCategoryId { get; set; }

    public string BlogCategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
}
