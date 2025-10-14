using System;
using System.Text.RegularExpressions;
using BLL.DTOs;

namespace BLL.Validators
{
    public static class BrandValidator
    {
        private static readonly Regex LettersAndSpaces =
            new Regex(@"^[\p{L}\s]+$", RegexOptions.Compiled);

        public static void Validate(BrandDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Thương hiệu không được null.");

            var name = (dto.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên thương hiệu không được để trống.");

            if (name.Length > 255)
                throw new ArgumentException("Tên thương hiệu không được vượt quá 255 ký tự.");

            if (!LettersAndSpaces.IsMatch(name))
                throw new ArgumentException("Tên thương hiệu chỉ được chứa chữ cái và khoảng trắng.");

            dto.Name = name;
        }
    }
}
