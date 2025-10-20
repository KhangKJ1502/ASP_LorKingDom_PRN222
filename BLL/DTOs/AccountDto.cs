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

        public string Password { get; set; } = null!;

        public bool IsDeleted { get; set; }

        public string Status { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Provider { get; set; }
    }
}
