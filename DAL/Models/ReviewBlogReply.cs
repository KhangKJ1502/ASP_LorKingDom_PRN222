using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class ReviewBlogReply
{
    public int ReplyBlogId { get; set; }

    public int ReviewBlogId { get; set; }

    public int AccountId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ReviewBlog ReviewBlog { get; set; } = null!;
}
