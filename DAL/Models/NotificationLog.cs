using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class NotificationLog
{
    public int LogId { get; set; }

    public int NotificationId { get; set; }

    public string? SentTo { get; set; }

    public string Result { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime SentAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;
}
