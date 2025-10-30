using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? TargetRoleId { get; set; }

    public int? TargetUserId { get; set; }

    public int CreatedBy { get; set; }

    public string? ConditionJson { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ExpireAt { get; set; }

    public bool IsSent { get; set; }

    public bool IsCanceled { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();

    public virtual Role? TargetRole { get; set; }

    public virtual Account? TargetUser { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
