using DAL.Interfaces;
using DAL.Models;

namespace BLL.Validators
{
    public class VoucherValidator
    {
        private readonly IVoucherRepository _repo;

        public VoucherValidator(IVoucherRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Validate voucher khi người dùng apply tại checkout
        /// </summary>
        public async Task<(bool isValid, string message, Voucher? voucher)> ValidateForApplyAsync(
            string code,
            int accountId,
            decimal orderAmount)
        {
            // 1. Kiểm tra mã rỗng
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Vui lòng nhập mã giảm giá", null);

            // 2. Kiểm tra voucher có tồn tại không
            var voucher = await _repo.GetByCodeAsync(code.Trim());
            if (voucher == null)
                return (false, "Mã giảm giá không tồn tại", null);

            // 3. Kiểm tra trạng thái Active
            if (voucher.Status != "Active")
                return (false, "Mã giảm giá không khả dụng", null);

            // 4. Kiểm tra thời hạn
            var now = DateTime.Now;
            if (now < voucher.StartDate)
                return (false, $"Mã giảm giá chưa có hiệu lực. Có hiệu lực từ {voucher.StartDate:dd/MM/yyyy}", null);

            if (now > voucher.EndDate)
                return (false, "Mã giảm giá đã hết hạn", null);

            // 5. Kiểm tra giá trị đơn hàng tối thiểu
            if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
                return (false, $"Đơn hàng tối thiểu {voucher.MinOrderAmount.Value:N0} ₫ để áp dụng mã này", null);

            // 6. Kiểm tra số lần sử dụng của user
            if (voucher.UsageLimitPerUser.HasValue)
            {
                var usageCount = await _repo.CountByVoucherAndAccountAsync(voucher.VoucherId, accountId);
                if (usageCount >= voucher.UsageLimitPerUser.Value)
                    return (false, "Bạn đã sử dụng hết lượt áp dụng mã này", null);
            }

            // 7. Tất cả điều kiện OK
            return (true, "Áp dụng mã giảm giá thành công", voucher);
        }
    }
}
