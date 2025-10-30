using System;
using System.Collections.Generic;

namespace BLL.Validators
{
    /// <summary>
    /// Validator cho Notification (Add, Edit, Delete)
    /// </summary>
    public class NotificationValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new();
            public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

            public void AddError(string error)
            {
                if (!string.IsNullOrWhiteSpace(error)) Errors.Add(error);
                IsValid = false;
            }
        }

        /// <summary>
        /// Validate Title (tiêu đề thông báo)
        /// </summary>
        public static ValidationResult ValidateTitle(string title)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(title))
            {
                result.AddError("Tiêu đề thông báo bắt buộc.");
                return result;
            }

            var t = title.Trim();
            if (t.Length < 3) result.AddError("Tiêu đề phải có ít nhất 3 ký tự.");
            if (t.Length > 255) result.AddError("Tiêu đề không được vượt quá 255 ký tự.");
            return result;
        }

        /// <summary>
        /// Validate Message (nội dung thông báo)
        /// </summary>
        public static ValidationResult ValidateMessage(string message)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(message))
            {
                result.AddError("Nội dung thông báo bắt buộc.");
                return result;
            }

            var m = message.Trim();
            if (m.Length < 5) result.AddError("Nội dung phải có ít nhất 5 ký tự.");
            if (m.Length > 2000) result.AddError("Nội dung không được vượt quá 2000 ký tự.");
            return result;
        }

        /// <summary>
        /// Validate Type (Promotional, SystemAlert, OrderUpdate, etc.)
        /// </summary>
        public static ValidationResult ValidateType(string type)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(type))
            {
                result.AddError("Loại thông báo bắt buộc.");
                return result;
            }

            var validTypes = new[] { "Promotional", "SystemAlert", "OrderUpdate", "General", "Urgent" };
            if (!Array.Exists(validTypes, t => t.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Loại thông báo không hợp lệ. Chỉ hỗ trợ: {string.Join(", ", validTypes)}");
            }
            return result;
        }

        /// <summary>
        /// Validate TargetType (All, Role, User)
        /// </summary>
        public static ValidationResult ValidateTargetType(string targetType)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(targetType))
            {
                result.AddError("Đối tượng nhận thông báo bắt buộc.");
                return result;
            }

            var validTargets = new[] { "All", "Role", "User" };
            if (!Array.Exists(validTargets, t => t.Equals(targetType.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Đối tượng nhận không hợp lệ. Chỉ hỗ trợ: {string.Join(", ", validTargets)}");
            }
            return result;
        }

        /// <summary>
        /// Validate ScheduledAt (ngày giờ gửi thông báo)
        /// </summary>
        public static ValidationResult ValidateScheduledAt(DateTime? scheduledAt)
        {
            var result = new ValidationResult();
            if (!scheduledAt.HasValue)
            {
                result.AddError("Thời gian gửi thông báo bắt buộc.");
                return result;
            }

            // Cho phép schedule trong quá khứ nếu muốn gửi ngay
            // Nếu muốn bắt buộc tương lai, uncomment dòng dưới:
            // if (scheduledAt.Value < DateTime.UtcNow)
            //     result.AddError("Thời gian gửi phải ở tương lai.");

            return result;
        }

        /// <summary>
        /// Validate ExpireAt (ngày hết hạn - optional nhưng phải sau ScheduledAt)
        /// </summary>
        public static ValidationResult ValidateExpireAt(DateTime? scheduledAt, DateTime? expireAt)
        {
            var result = new ValidationResult();
            if (!expireAt.HasValue) return result; // Optional

            if (!scheduledAt.HasValue)
            {
                result.AddError("Không thể xác định ngày hết hạn khi chưa có thời gian gửi.");
                return result;
            }

            if (expireAt.Value <= scheduledAt.Value)
            {
                result.AddError("Thời gian hết hạn phải sau thời gian gửi.");
            }

            return result;
        }

        /// <summary>
        /// Validate TargetRoleId khi TargetType = Role
        /// </summary>
        public static ValidationResult ValidateTargetRoleId(string targetType, int? targetRoleId)
        {
            var result = new ValidationResult();
            if (targetType?.Trim().Equals("Role", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!targetRoleId.HasValue || targetRoleId.Value <= 0)
                {
                    result.AddError("Vui lòng chọn vai trò khi gửi thông báo theo role.");
                }
            }
            return result;
        }

        /// <summary>
        /// Validate TargetUserId khi TargetType = User
        /// </summary>
        public static ValidationResult ValidateTargetUserId(string targetType, int? targetUserId)
        {
            var result = new ValidationResult();
            if (targetType?.Trim().Equals("User", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!targetUserId.HasValue || targetUserId.Value <= 0)
                {
                    result.AddError("Vui lòng chọn người dùng khi gửi thông báo cá nhân.");
                }
            }
            return result;
        }

        /// <summary>
        /// Validate toàn bộ cho Create Notification
        /// </summary>
        public static ValidationResult ValidateCreate(
            string title,
            string message,
            string type,
            string targetType,
            DateTime? scheduledAt,
            DateTime? expireAt,
            int? targetRoleId,
            int? targetUserId)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateTitle(title).Errors);
            result.Errors.AddRange(ValidateMessage(message).Errors);
            result.Errors.AddRange(ValidateType(type).Errors);
            result.Errors.AddRange(ValidateTargetType(targetType).Errors);
            result.Errors.AddRange(ValidateScheduledAt(scheduledAt).Errors);
            result.Errors.AddRange(ValidateExpireAt(scheduledAt, expireAt).Errors);
            result.Errors.AddRange(ValidateTargetRoleId(targetType, targetRoleId).Errors);
            result.Errors.AddRange(ValidateTargetUserId(targetType, targetUserId).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate toàn bộ cho Update Notification
        /// </summary>
        public static ValidationResult ValidateUpdate(
            int notificationId,
            string title,
            string message,
            string type,
            string targetType,
            DateTime? scheduledAt,
            DateTime? expireAt,
            int? targetRoleId,
            int? targetUserId)
        {
            var result = new ValidationResult();

            if (notificationId <= 0)
                result.AddError("ID thông báo không hợp lệ.");

            result.Errors.AddRange(ValidateTitle(title).Errors);
            result.Errors.AddRange(ValidateMessage(message).Errors);
            result.Errors.AddRange(ValidateType(type).Errors);
            result.Errors.AddRange(ValidateTargetType(targetType).Errors);
            result.Errors.AddRange(ValidateScheduledAt(scheduledAt).Errors);
            result.Errors.AddRange(ValidateExpireAt(scheduledAt, expireAt).Errors);
            result.Errors.AddRange(ValidateTargetRoleId(targetType, targetRoleId).Errors);
            result.Errors.AddRange(ValidateTargetUserId(targetType, targetUserId).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate ID cho Delete/Cancel/SendNow
        /// </summary>
        public static ValidationResult ValidateId(int id)
        {
            var result = new ValidationResult();
            if (id <= 0)
                result.AddError("ID thông báo không hợp lệ.");
            return result;
        }
    }
}
