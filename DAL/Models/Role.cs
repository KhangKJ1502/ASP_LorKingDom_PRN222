using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<NotificationMessage> NotificationMessages { get; set; } = new List<NotificationMessage>();
}
