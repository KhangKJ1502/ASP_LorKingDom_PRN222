using BLL.DTOs;

namespace BLL.Validators
{
    public static class AddressValidator
    {
        public static void Validate(string city, string ward, string addressLine)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("Thành phố không được để trống.");

            if (string.IsNullOrWhiteSpace(ward))
                throw new ArgumentException("Phường/Xã không được để trống.");

            if (string.IsNullOrWhiteSpace(addressLine))
                throw new ArgumentException("Địa chỉ chi tiết không được để trống.");

            if (string.IsNullOrWhiteSpace(ward))
                throw new ArgumentException("Phường/Xã không được để trống.");

            // Ward có thể rỗng, nhưng không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(ward) && ward.Length > 255)
                throw new ArgumentException("Phường/Xã không được vượt quá 255 ký tự.");

            if (city.Length > 255)
                throw new ArgumentException("Tên thành phố không được vượt quá 255 ký tự.");

            if (addressLine.Length > 255)
                throw new ArgumentException("Địa chỉ chi tiết không được vượt quá 255 ký tự.");
        }

        public static void Validate(AddressDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Dữ liệu địa chỉ không được để trống.");

            Validate(dto.City ?? "", dto.Ward ?? "", dto.AddressLine ?? "");
        }
    }
}
