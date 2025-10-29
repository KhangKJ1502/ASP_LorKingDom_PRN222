using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class ReviewBlog
{
    public int ReviewBlogId { get; set; }

    public int BlogPostId { get; set; }

    public int AccountId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual ICollection<ReviewBlogReaction> ReviewBlogReactions { get; set; } = new List<ReviewBlogReaction>();

    public virtual ICollection<ReviewBlogReply> ReviewBlogReplies { get; set; } = new List<ReviewBlogReply>();
}
