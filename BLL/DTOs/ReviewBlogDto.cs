namespace BLL.DTOs
{
    public sealed class ReviewBlogDto
    {
        public int ReviewBlogId { get; set; }
        public int BlogPostId { get; set; }
        public int AccountId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }

        // Display fields
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string? AuthorImage { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public bool? CurrentUserReactionType { get; set; } // null = no reaction, true = like, false = dislike
        public List<ReviewBlogReplyDto> Replies { get; set; } = new();
    }
}
