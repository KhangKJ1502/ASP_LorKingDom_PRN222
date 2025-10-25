namespace BLL.DTOs
{
    public sealed class AddressDto
    {
        public int AddressId { get; set; }
        public string AddressLine { get; set; } = "";
        public string City { get; set; } = "";
        public string? Ward { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
