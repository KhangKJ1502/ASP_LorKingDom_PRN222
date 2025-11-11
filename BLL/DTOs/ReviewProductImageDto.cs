namespace BLL.DTOs
{
    public class ReviewProductImageDto
    {
        public int ReviewProductImageId { get; set; }
        public int ReviewProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}