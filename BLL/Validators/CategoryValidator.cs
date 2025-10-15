using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BLL.Validators
{
    public static class CategoryValidator
    {
        private static readonly Regex LettersAndSpaces =
            new Regex(@"^[\p{L}\s]+$", RegexOptions.Compiled);

        public static void Validate(CategoryDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Danh mục không được null.");

            var name = (dto.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên danh mục không được để trống.");

            if (name.Length > 255)
                throw new ArgumentException("Tên danh mục không được vượt quá 255 ký tự.");

            if (!LettersAndSpaces.IsMatch(name))
                throw new ArgumentException("Tên danh mục chỉ được chứa chữ cái và khoảng trắng.");

            dto.Name = name;
        }
    }
}
