using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class EmailOtp
{
    public int EmailOtpId { get; set; }

    public int? AccountId { get; set; }

    public string? Email { get; set; }

    public string Purpose { get; set; } = null!;

    public string OtpCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public virtual Account? Account { get; set; }
}
