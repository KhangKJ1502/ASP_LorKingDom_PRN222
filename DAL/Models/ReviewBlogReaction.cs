using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class ReviewBlogReaction
{
    public int ReactionBlogId { get; set; }

    public int ReviewBlogId { get; set; }

    public int AccountId { get; set; }

    public string ReactionType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ReviewBlog ReviewBlog { get; set; } = null!;
}
