using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class NotificationRecipient
{
    public int NotificationId { get; set; }

    public int AccountId { get; set; }

    public string ReadStatus { get; set; } = null!;

    public DateTime? ReadAt { get; set; }

    public bool Archived { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual NotificationMessage Notification { get; set; } = null!;
}
