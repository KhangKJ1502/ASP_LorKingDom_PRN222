namespace BLL.DTOs
{
    public sealed class AccountDto
    {
        public int Id { get; set; }
        public int? RoleId { get; set; }

        public string AccountName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = null!;
        public string? Image { get; set; }

        // Với Create: là plaintext; với Read/Update: là hash (đã xử lý ở Service)
        public string? Password { get; set; } // nullable cho Update

        public bool IsDeleted { get; set; }
        public string Status { get; set; } = "Active"; // "Active" | "Inactive"

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Provider { get; set; } // "Local" | "Google" | "Facebook"
    }
}
