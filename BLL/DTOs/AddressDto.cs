namespace BLL.DTOs;
public class AddressDto
{
    public int AddressId { get; set; }
    public string AddressLine { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? Ward { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
