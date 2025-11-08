using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _repo;
        private readonly VoucherValidator _validator;

        public VoucherService(IVoucherRepository repo)
        {
            _repo = repo;
            _validator = new VoucherValidator(repo);
        }

        public async Task<List<VoucherDto>> GetAllVouchersAsync(bool includeDeleted = false)
        {
            var vouchers = await _repo.GetAllAsync();
            return vouchers
                .Where(v => includeDeleted || v.Status != "Inactive")
                .Select(MapToDto)
                .ToList();
        }

        public async Task<VoucherDto?> GetByIdAsync(int voucherId)
        {
            var voucher = await _repo.GetByIdAsync(voucherId);
            return voucher != null ? MapToDto(voucher) : null;
        }

        public async Task<int> CreateAsync(VoucherDto dto)
        {
            // Validation cơ bản
            if (string.IsNullOrWhiteSpace(dto.VoucherCode))
                throw new ArgumentException("Mã voucher không được để trống.");

            if (await _repo.VoucherCodeExistsAsync(dto.VoucherCode))
                throw new ArgumentException("Mã voucher đã tồn tại.");

            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

            if (dto.DiscountValue <= 0)
                throw new ArgumentException("Giá trị giảm giá phải lớn hơn 0.");

            // VALIDATION MỚI: Số tiền giảm phải nhỏ hơn đơn hàng tối thiểu
            if (dto.MinOrderAmount.HasValue && dto.MinOrderAmount.Value > 0)
            {
                if (dto.DiscountValue >= dto.MinOrderAmount.Value)
                {
                    throw new ArgumentException($"Số tiền giảm ({dto.DiscountValue:N0} VND) phải nhỏ hơn đơn hàng tối thiểu ({dto.MinOrderAmount.Value:N0} VND).");
                }
            }

            var voucher = new Voucher
            {
                VoucherTypeId = dto.VoucherTypeId,
                CreateBy = dto.CreateBy,
                VoucherCode = dto.VoucherCode.Trim().ToUpper(),
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = 0, // Mặc định = 0 vì không dùng giảm theo %
                MinOrderAmount = dto.MinOrderAmount,
                UsageLimitPerUser = dto.UsageLimitPerUser,
                IsStackable = dto.IsStackable,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status ?? "Active",
                CreatedAt = DateTime.Now
            };

            return await _repo.CreateAsync(voucher);
        }

        public async Task<bool> UpdateAsync(int id, VoucherDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.Status == "Inactive")
                return false;

            // Validation
            if (await _repo.VoucherCodeExistsAsync(dto.VoucherCode, id))
                throw new ArgumentException("Mã voucher đã tồn tại.");

            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

            if (dto.DiscountValue <= 0)
                throw new ArgumentException("Giá trị giảm giá phải lớn hơn 0.");

            // VALIDATION MỚI: Số tiền giảm phải nhỏ hơn đơn hàng tối thiểu
            if (dto.MinOrderAmount.HasValue && dto.MinOrderAmount.Value > 0)
            {
                if (dto.DiscountValue >= dto.MinOrderAmount.Value)
                {
                    throw new ArgumentException($"Số tiền giảm ({dto.DiscountValue:N0} VND) phải nhỏ hơn đơn hàng tối thiểu ({dto.MinOrderAmount.Value:N0} VND).");
                }
            }

            existing.VoucherTypeId = dto.VoucherTypeId;
            existing.CreateBy = dto.CreateBy;
            existing.VoucherCode = dto.VoucherCode.Trim().ToUpper();
            existing.DiscountValue = dto.DiscountValue;
            existing.MaxDiscountAmount = 0; // Mặc định = 0
            existing.MinOrderAmount = dto.MinOrderAmount;
            existing.UsageLimitPerUser = dto.UsageLimitPerUser;
            existing.IsStackable = dto.IsStackable;
            existing.StartDate = dto.StartDate;
            existing.EndDate = dto.EndDate;
            existing.Status = dto.Status ?? existing.Status; // Cho phép cập nhật status
            existing.UpdatedAt = DateTime.Now;

            return await _repo.UpdateAsync(existing);
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var voucher = await _repo.GetByIdAsync(id);
            if (voucher == null || voucher.Status == "Inactive") return false;

            voucher.Status = "Inactive";
            voucher.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(voucher);
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var voucher = await _repo.GetByIdAsync(id);
            if (voucher == null || voucher.Status != "Inactive") return false;

            voucher.Status = (voucher.EndDate < DateTime.Now) ? "Expired" : "Active";
            voucher.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(voucher);
        }

        public async Task<(bool isValid, string message, VoucherDto? voucher)> ApplyVoucherAsync(string code, int accountId, decimal orderAmount)
        {
            var (isValid, message, voucherEntity) = await _validator.ValidateForApplyAsync(code, accountId, orderAmount);

            if (!isValid || voucherEntity == null)
                return (isValid, message, null);

            var voucherDto = MapToDto(voucherEntity);
            return (true, message, voucherDto);
        }

        private VoucherDto MapToDto(Voucher voucher)
        {
            return new VoucherDto
            {
                VoucherId = voucher.VoucherId,
                VoucherTypeId = voucher.VoucherTypeId,
                VoucherTypeName = voucher.VoucherType?.VoucherTypeName ?? "Unknown",
                CreateBy = voucher.CreateBy,
                CreatorName = voucher.CreateByNavigation?.AccountName ?? "Unknown",
                VoucherCode = voucher.VoucherCode,
                DiscountValue = voucher.DiscountValue,
                MaxDiscountAmount = voucher.MaxDiscountAmount,
                MinOrderAmount = voucher.MinOrderAmount,
                UsageLimitPerUser = voucher.UsageLimitPerUser,
                IsStackable = voucher.IsStackable,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                Status = voucher.Status,
                IsDeleted = voucher.Status == "Inactive",
                CreatedAt = voucher.CreatedAt,
                UpdatedAt = voucher.UpdatedAt
            };
        }
    }
}