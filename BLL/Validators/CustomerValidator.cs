using System;
using System.Collections.Generic;

namespace BLL.Validators
{
    /// <summary>
    /// Validator cho Customer Account Management (View, Edit, Block)
    /// </summary>
    public class CustomerValidator
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
        /// Validate Customer ID
        /// </summary>
        public static ValidationResult ValidateId(int id)
        {
            var result = new ValidationResult();
            if (id <= 0)
                result.AddError("ID khách hàng không hợp lệ.");
            return result;
        }

        /// <summary>
        /// Validate Customer Name
        /// </summary>
        public static ValidationResult ValidateCustomerName(string name)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("Tên khách hàng bắt buộc.");
                return result;
            }

            var n = name.Trim();
            if (n.Length < 2) result.AddError("Tên khách hàng phải có ít nhất 2 ký tự.");
            if (n.Length > 100) result.AddError("Tên khách hàng không được vượt quá 100 ký tự.");
            return result;
        }

        /// <summary>
        /// Validate Block/Unblock reason
        /// </summary>
        public static ValidationResult ValidateBlockReason(string reason, bool isBlocking)
        {
            var result = new ValidationResult();
            
            if (isBlocking && string.IsNullOrWhiteSpace(reason))
            {
                result.AddError("Lý do khóa tài khoản bắt buộc.");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var r = reason.Trim();
                if (r.Length < 5) result.AddError("Lý do phải có ít nhất 5 ký tự.");
                if (r.Length > 500) result.AddError("Lý do không được vượt quá 500 ký tự.");
            }

            return result;
        }

        /// <summary>
        /// Validate Edit Customer (admin chỉnh sửa thông tin customer)
        /// </summary>
        public static ValidationResult ValidateEdit(int id, string name, string phoneNumber)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateId(id).Errors);
            result.Errors.AddRange(ValidateCustomerName(name).Errors);
            
            // Reuse AccountValidator for phone number
            result.Errors.AddRange(AccountValidator.ValidatePhoneNumber(phoneNumber).Errors);
            
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate Block Customer
        /// </summary>
        public static ValidationResult ValidateBlock(int id, string reason)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateId(id).Errors);
            result.Errors.AddRange(ValidateBlockReason(reason, isBlocking: true).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Validate Unblock Customer
        /// </summary>
        public static ValidationResult ValidateUnblock(int id)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(ValidateId(id).Errors);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
}
