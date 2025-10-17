using System;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs
{
    public class NotificationCreateDto
    {
        [Required] public int CreatedBy { get; set; }
        public string? ConditionJson { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, StringLength(4000)]
        public string Message { get; set; } = null!;

        [Required, StringLength(50)]
        public string Type { get; set; } = "system"; // info/promo/system

        [Required, StringLength(50)]
        public string TargetType { get; set; } = "All"; // All/Role/User/Condition

        public int? TargetRoleId { get; set; }
        public int? TargetUserId { get; set; }

        [Required] public DateTime ScheduledAt { get; set; }

        public DateTime? ExpireAt { get; set; }
    }

    public class NotificationUpdateDto : NotificationCreateDto
    {
        [Required] public int NotificationId { get; set; }
    }

    public class NotificationDto
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
    }

    public class NotificationFilterDto
    {
        public string? Keyword { get; set; }
        public string? Type { get; set; }
        public string? TargetType { get; set; }
        public int? TargetRoleId { get; set; }
        public int? TargetUserId { get; set; }
        public bool? IsSent { get; set; }
        public bool? IsCanceled { get; set; }
        public DateTime? ScheduledFrom { get; set; }
        public DateTime? ScheduledTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class NotificationLogDto
    {
        public int LogId { get; set; }
        public int NotificationId { get; set; }
        public string? SentTo { get; set; }
        public string Result { get; set; } = null!;
        public string? Details { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class UserNotificationDto
    {
        public int UserNotificationId { get; set; }
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // View helpers
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime ScheduledAt { get; set; }
    }

    /// <summary>
    /// Dùng chung cho cả Create và Update để lưu Notification.
    /// </summary>
    public class NotificationSaveDto
    {
        public int? NotificationId { get; set; } // null khi tạo mới, có giá trị khi update

        [Required]
        public int CreatedBy { get; set; }

        public string? ConditionJson { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, StringLength(4000)]
        public string Message { get; set; } = null!;

        [Required, StringLength(50)]
        public string Type { get; set; } = "system"; // info/promo/system

        [Required, StringLength(50)]
        public string TargetType { get; set; } = "All"; // All/Role/User/Condition

        public int? TargetRoleId { get; set; }
        public int? TargetUserId { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? ExpireAt { get; set; }
    }
}
