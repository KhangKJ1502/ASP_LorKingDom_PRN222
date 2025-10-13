using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.Validators
{
    public static class PriceRangeValidator
    {
        public static void Validate(PriceRangeDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Khoảng giá không được null.");

            if (dto.PriceRangeMin < 0 || dto.PriceRangeMax < 0)
                throw new ArgumentException("Giá phải lớn hơn hoặc bằng 0.");

            if (dto.PriceRangeMin >= dto.PriceRangeMax)
                throw new ArgumentException("Giá tối thiểu phải nhỏ hơn giá tối đa.");
        }
    }
}
