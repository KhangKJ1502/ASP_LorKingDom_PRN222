using System;

namespace BLL.DTOs
{
    public sealed class AddressDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AddressLine { get; set; } = "";
        public string City { get; set; } = "";
        public string? Ward { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
