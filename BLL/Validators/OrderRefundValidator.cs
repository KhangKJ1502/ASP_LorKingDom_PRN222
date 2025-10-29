using System;
using System.Collections.Generic;

namespace BLL.Validators
{
    /// <summary>
    /// Validator cho Order Refund (Approve/Reject refund requests)
    /// </summary>
    public class OrderRefundValidator
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
        /// Validate RefundId
        /// </summary>
        public static ValidationResult ValidateRefundId(long refundId)
        {
            var result = new ValidationResult();
            if (refundId <= 0)
                result.AddError("ID yêu cầu hoàn tiền không hợp lệ.");
            return result;
        }

        /// <summary>
        /// Validate Refund Status
        /// Valid statuses: Pending, Approved, Rejected, Processing, Completed, Cancelled
        /// </summary>
        public static ValidationResult ValidateStatus(string status)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(status))
            {
                result.AddError("Trạng thái hoàn tiền bắt buộc.");
                return result;
            }

            var validStatuses = new[] 
            { 
                "Pending",      // Chờ duyệt
                "Approved",     // Đã duyệt
                "Rejected",     // Từ chối
                "Processing",   // Đang xử lý
                "Completed",    // Hoàn thành
                "Cancelled"     // Đã hủy
            };

            if (!Array.Exists(validStatuses, s => s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Trạng thái không hợp lệ. Chỉ hỗ trợ: {string.Join(", ", validStatuses)}");
            }

            return result;
        }

        /// <summary>
        /// Validate Refund Reason (khi customer request refund)
        /// </summary>
        public static ValidationResult ValidateReason(string reason)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(reason))
            {
                result.AddError("Lý do hoàn tiền bắt buộc.");
                return result;
            }

            var r = reason.Trim();
            if (r.Length < 10) result.AddError("Lý do hoàn tiền phải có ít nhất 10 ký tự.");
            if (r.Length > 1000) result.AddError("Lý do hoàn tiền không được vượt quá 1000 ký tự.");
            return result;
        }

        /// <summary>
        /// Validate Refund Amount (số tiền hoàn lại)
        /// </summary>
        public static ValidationResult ValidateRefundAmount(decimal? refundAmount, decimal? orderAmount)
        {
            var result = new ValidationResult();
            if (!refundAmount.HasValue || refundAmount.Value <= 0)
            {
                result.AddError("Số tiền hoàn lại phải lớn hơn 0.");
                return result;
            }

            if (orderAmount.HasValue && refundAmount.Value > orderAmount.Value)
            {
                result.AddError("Số tiền hoàn lại không được vượt quá giá trị đơn hàng.");
            }

            return result;
        }

        /// <summary>
        /// Validate Admin Note (ghi chú khi duyệt/từ chối)
        /// </summary>
        public static ValidationResult ValidateAdminNote(string adminNote, bool isRequired = false)
        {
            var result = new ValidationResult();
            if (isRequired && string.IsNullOrWhiteSpace(adminNote))
            {
                result.AddError("Ghi chú của admin bắt buộc khi từ chối yêu cầu.");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                var note = adminNote.Trim();
                if (note.Length > 500) result.AddError("Ghi chú không được vượt quá 500 ký tự.");
            }

            return result;
        }

        /// <summary>
        /// Validate StaffId (người duyệt)
        /// </summary>
        public static ValidationResult ValidateStaffId(int staffId)
        {
            var result = new ValidationResult();
            if (staffId <= 0)
                result.AddError("Không thể xác định nhân viên xử lý yêu cầu.");
            return result;
        }

        /// <summary>
        /// Validate Request Date (ngày yêu cầu hoàn tiền)
        /// </summary>
        public static ValidationResult ValidateRequestDate(DateTime? requestDate)
        {
            var result = new ValidationResult();
            if (!requestDate.HasValue)
            {
                result.AddError("Ngày yêu cầu hoàn tiền bắt buộc.");
                return result;
            }

            // Không cho phép request date trong tương lai
            if (requestDate.Value > DateTime.UtcNow.AddHours(1)) // +1h để tránh timezone issues
            {
                result.AddError("Ngày yêu cầu hoàn tiền không thể ở tương lai.");
            }

            return result;
        }

        /// <summary>
        /// Validate Status Transition (kiểm tra chuyển trạng thái hợp lệ)
        /// </summary>
        public static ValidationResult ValidateStatusTransition(string currentStatus, string newStatus)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(newStatus))
            {
                result.AddError("Trạng thái hiện tại và trạng thái mới bắt buộc.");
                return result;
            }

            var current = currentStatus.Trim();
            var next = newStatus.Trim();

            // Business rules cho phép chuyển trạng thái
            var allowedTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Pending", new[] { "Approved", "Rejected", "Processing" } },
                { "Approved", new[] { "Processing", "Completed", "Cancelled" } },
                { "Processing", new[] { "Completed", "Cancelled" } },
                { "Rejected", new[] { "Pending" } }, // Có thể xem xét lại
                { "Completed", Array.Empty<string>() }, // Không cho phép thay đổi khi đã hoàn thành
                { "Cancelled", Array.Empty<string>() }  // Không cho phép thay đổi khi đã hủy
            };

            if (!allowedTransitions.ContainsKey(current))
            {
                result.AddError($"Trạng thái hiện tại '{current}' không hợp lệ.");
                return result;
            }

            if (!Array.Exists(allowedTransitions[current], s => s.Equals(next, StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Không thể chuyển từ trạng thái '{current}' sang '{next}'.");
            }

            return result;
        }

        /// <summary>
        /// Validate Approve Request (duyệt yêu cầu hoàn tiền)
        /// </summary>
        public static ValidationResult ValidateApprove(
            long refundId,
            int staffId,
            string currentStatus,
            string adminNote = "")
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateRefundId(refundId).Errors);
            result.Errors.AddRange(ValidateStaffId(staffId).Errors);
            result.Errors.AddRange(ValidateStatusTransition(currentStatus, "Approved").Errors);
            result.Errors.AddRange(ValidateAdminNote(adminNote, isRequired: false).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate Reject Request (từ chối yêu cầu hoàn tiền)
        /// </summary>
        public static ValidationResult ValidateReject(
            long refundId,
            int staffId,
            string currentStatus,
            string adminNote)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateRefundId(refundId).Errors);
            result.Errors.AddRange(ValidateStaffId(staffId).Errors);
            result.Errors.AddRange(ValidateStatusTransition(currentStatus, "Rejected").Errors);
            
            // Admin note BẮT BUỘC khi từ chối
            result.Errors.AddRange(ValidateAdminNote(adminNote, isRequired: true).Errors);
            
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate Create Refund Request (customer tạo yêu cầu hoàn tiền)
        /// </summary>
        public static ValidationResult ValidateCreateRequest(
            long orderId,
            int customerId,
            string reason,
            decimal refundAmount,
            decimal orderAmount)
        {
            var result = new ValidationResult();

            if (orderId <= 0)
                result.AddError("ID đơn hàng không hợp lệ.");

            if (customerId <= 0)
                result.AddError("ID khách hàng không hợp lệ.");

            result.Errors.AddRange(ValidateReason(reason).Errors);
            result.Errors.AddRange(ValidateRefundAmount(refundAmount, orderAmount).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate Update Status (cập nhật trạng thái chung)
        /// </summary>
        public static ValidationResult ValidateUpdateStatus(
            long refundId,
            string currentStatus,
            string newStatus,
            int staffId)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateRefundId(refundId).Errors);
            result.Errors.AddRange(ValidateStatus(newStatus).Errors);
            result.Errors.AddRange(ValidateStatusTransition(currentStatus, newStatus).Errors);
            result.Errors.AddRange(ValidateStaffId(staffId).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
}
