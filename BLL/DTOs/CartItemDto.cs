using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtThatTime { get; set; }
        public string Status { get; set; } = null!;
        public DateTime AddedAt { get; set; }

        public string ProductName { get; set; } = null!;
        public string MainImageUrl { get; set; } = null!;
        public decimal CurrentPrice { get; set; }
        public int ProductQuantity { get; set; }
    }
}