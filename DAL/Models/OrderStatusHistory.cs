using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class OrderStatusHistory
{
    public int OrderStatusHistoryId { get; set; }

    public int OrderId { get; set; }

    public int? StatusId { get; set; }

    public DateTime ChangedAt { get; set; }

    public int? ChangedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? ChangedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual StatusOrder? Status { get; set; }
}
