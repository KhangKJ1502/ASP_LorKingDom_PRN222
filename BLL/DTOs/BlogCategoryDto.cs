namespace BLL.DTOs
{
    public sealed class BlogCategoryDto
    {
        public int BlogCategoryId { get; set; }
        public string BlogCategoryName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
