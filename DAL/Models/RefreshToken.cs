using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public int AccountId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
