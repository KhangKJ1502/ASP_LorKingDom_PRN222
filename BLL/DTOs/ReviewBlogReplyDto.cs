namespace BLL.DTOs
{
    public sealed class ReviewBlogReplyDto
    {
        public int ReviewBlogReplyId { get; set; }
        public int ReplyBlogId { get; set; }
        public int ReviewBlogId { get; set; }
        public int AccountId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Display fields
        public string? AuthorName { get; set; }
        public string? AuthorImage { get; set; }
        public bool IsAdmin { get; set; } // Để biết là admin phản hồi hay không
    }
}
