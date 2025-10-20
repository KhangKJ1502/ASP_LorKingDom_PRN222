using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    /// <summary>Item gọn cho combobox (Value, Text).</summary>
    public record SelectItemDto(string Value, string Text);
}