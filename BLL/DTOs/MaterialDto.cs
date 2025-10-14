using System;

namespace BLL.DTOs
{
    public sealed class MaterialDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
