using System;
using System.Text.RegularExpressions;
using BLL.DTOs;

namespace BLL.Validators
{
    public static class MaterialValidator
    {
        private static readonly Regex LettersAndSpaces =
            new Regex(@"^[\p{L}\s]+$", RegexOptions.Compiled);

        public static void Validate(MaterialDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Chất liệu không được null.");

            var name = (dto.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên chất liệu không được để trống.");

            if (name.Length > 255)
                throw new ArgumentException("Tên chất liệu không được vượt quá 255 ký tự.");

            if (!LettersAndSpaces.IsMatch(name))
                throw new ArgumentException("Tên chất liệu chỉ được chứa chữ cái và khoảng trắng.");

            if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 255)
                throw new ArgumentException("Mô tả không được vượt quá 255 ký tự.");

            dto.Name = name;
        }
    }
}
