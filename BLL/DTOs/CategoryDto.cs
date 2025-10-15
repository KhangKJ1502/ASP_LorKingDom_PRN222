using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public sealed class CategoryDto
    {
        public int Id { get; set; }
        public int SuperCategoryId { get; set; }
        public string Name { get; set; } = "";
        public bool IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? SuperCategoryName { get; set; } // dùng để hiển thị trong bảng
    }
}
