using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
	public class AddToCartRequestDto
	{
		public int Id { get; set; }
		public int Qty { get; set; } = 1;
	}
}
