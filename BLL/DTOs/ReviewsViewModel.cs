namespace BLL.DTOs
{
    public class ReviewsViewModel
    {
        public List<PendingReviewDto> PendingReviews { get; set; } = new();
        public List<ReviewProductDto> MyReviews { get; set; } = new();
    }

    public class PendingReviewDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int OrderId { get; set; }
        public decimal Price { get; set; }
    }
}