using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BLL.Validators
{
    public static class OriginValidator
    {
        private static readonly Regex LettersAndSpaces =
            new Regex(@"^[\p{L}\s]+$", RegexOptions.Compiled);

        public static void Validate(OriginDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Xuất xứ không được null.");

            var name = (dto.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên xuất xứ không được để trống.");

            if (name.Length > 255)
                throw new ArgumentException("Tên xuất xứ không được vượt quá 255 ký tự.");

            if (!LettersAndSpaces.IsMatch(name))
                throw new ArgumentException("Tên xuất xứ chỉ được chứa chữ cái và khoảng trắng.");

            dto.Name = name;
        }
    }
}
