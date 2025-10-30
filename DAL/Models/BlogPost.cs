using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class BlogPost
{
    public int BlogPostId { get; set; }

    public int AccountId { get; set; }

    public string BlogTitle { get; set; } = null!;

    public string BlogContent { get; set; } = null!;

    public string? BlogThumbnail { get; set; }

    public string? BlogUrl { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<ReviewBlog> ReviewBlogs { get; set; } = new List<ReviewBlog>();

    public virtual ICollection<BlogCategory> BlogCategories { get; set; } = new List<BlogCategory>();
}
