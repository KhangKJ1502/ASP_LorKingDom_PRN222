namespace BLL.DTOs
{
    public sealed class ReviewProductReactionDto
    {
        public int ReactionProductId { get; set; }
        public int ReviewProductId { get; set; }
        public int AccountId { get; set; }
        public string ReactionType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Display fields
        public string? AuthorName { get; set; }
    }
}