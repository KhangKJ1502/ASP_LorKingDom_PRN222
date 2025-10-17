using System;

namespace BLL.DTOs
{
    public sealed class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
