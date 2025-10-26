namespace BLL.DTOs
{
    public sealed class ReviewProductReplyDto
    {
        public int ReplyProductId { get; set; }
        public int ReviewProductId { get; set; }
        public int AccountId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Display fields
        public string? AuthorName { get; set; }
        public bool IsAdmin { get; set; }
    }
}