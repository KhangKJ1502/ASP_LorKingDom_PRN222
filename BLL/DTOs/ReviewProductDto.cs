namespace BLL.DTOs
{
    public sealed class ReviewProductDto
    {
        public int ReviewProductId { get; set; }
        public int ProductId { get; set; }
        public int AccountId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Display fields
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string? ProductName { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public string? UserReaction { get; set; }       
        public List<ReviewProductReplyDto> Replies { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
    }
}