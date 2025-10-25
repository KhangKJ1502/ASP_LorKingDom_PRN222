using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _repo;

        public VoucherService(IVoucherRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<VoucherDto>> GetAllVouchersAsync(bool includeDeleted = false)
        {
            var vouchers = await _repo.GetAllAsync();
            return vouchers
                .Where(v => v.Status != "Deleted")
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
            if (string.IsNullOrWhiteSpace(dto.VoucherCode))
                throw new ArgumentException("Voucher code cannot be empty.");
            if (await _repo.VoucherCodeExistsAsync(dto.VoucherCode))
                throw new ArgumentException("Voucher code already exists.");
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");
            if (dto.DiscountValue <= 0)
                throw new ArgumentException("Discount value must be positive.");

            var voucher = new Voucher
            {
                VoucherTypeId = dto.VoucherTypeId,
                CreateBy = dto.CreateBy,
                VoucherCode = dto.VoucherCode.Trim(),
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
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
            if (existing == null || existing.Status == "Deleted")
                return false;

            if (await _repo.VoucherCodeExistsAsync(dto.VoucherCode, id))
                throw new ArgumentException("Voucher code already exists.");
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");
            if (dto.DiscountValue <= 0)
                throw new ArgumentException("Discount value must be positive.");

            existing.VoucherTypeId = dto.VoucherTypeId;
            existing.CreateBy = dto.CreateBy;
            existing.VoucherCode = dto.VoucherCode.Trim();
            existing.DiscountValue = dto.DiscountValue;
            existing.MaxDiscountAmount = dto.MaxDiscountAmount;
            existing.MinOrderAmount = dto.MinOrderAmount;
            existing.UsageLimitPerUser = dto.UsageLimitPerUser;
            existing.IsStackable = dto.IsStackable;
            existing.StartDate = dto.StartDate;
            existing.EndDate = dto.EndDate;
            existing.Status = dto.Status ?? "Active";
            existing.UpdatedAt = DateTime.Now;

            return await _repo.UpdateAsync(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id); // XÓA CỨNG
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
                IsDeleted = voucher.Status == "Deleted",
                CreatedAt = voucher.CreatedAt,
                UpdatedAt = voucher.UpdatedAt
            };
        }
    }
}