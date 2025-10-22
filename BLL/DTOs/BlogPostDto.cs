namespace BLL.DTOs
{
    public sealed class BlogPostDto
    {
        public int BlogPostId { get; set; }
        public int AccountId { get; set; }
        public string BlogTitle { get; set; } = null!;
        public string BlogContent { get; set; } = null!;
        public string? BlogThumbnail { get; set; }
        public string? BlogUrl { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? AuthorName { get; set; }
        public List<int> CategoryIds { get; set; } = new List<int>();
        public List<string> CategoryNames { get; set; } = new List<string>();
    }
}
