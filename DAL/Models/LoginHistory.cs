namespace DAL.Models
{
    public partial class LoginHistory
    {
        public int LoginId { get; set; }
        public int AccountId { get; set; }
        public DateTime LoginTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
