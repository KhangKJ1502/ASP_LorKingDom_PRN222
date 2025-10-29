using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class ExternalLogin
{
    public int ExternalLoginId { get; set; }

    public int AccountId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderKey { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
