namespace BLL.DTOs
{
    public sealed class ReviewBlogReactionDto
    {
        public int ReactionBlogId { get; set; }
        public int ReviewBlogId { get; set; }
        public int AccountId { get; set; }
        public string ReactionType { get; set; } = null!; // "like" or "dislike"
        public DateTime CreatedAt { get; set; }

        // Display fields
        public string? AuthorName { get; set; }
    }
}
