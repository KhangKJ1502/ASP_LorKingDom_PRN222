using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class NotificationMessage
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string Type { get; set; } = null!;

    public byte Priority { get; set; }

    public string? DeepLinkUrl { get; set; }

    public bool IsBroadcast { get; set; }

    public int? TargetRoleId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? ExpireAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? CreatedByNavigation { get; set; }

    public virtual ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();

    public virtual Role? TargetRole { get; set; }
}
