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
            vouchers = vouchers.Where(v => includeDeleted || v.Status != "Deleted").ToList();
            return vouchers.Select(v => MapToDto(v)).ToList();
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
            var existingVoucher = await _repo.GetByIdAsync(id);
            if (existingVoucher == null || existingVoucher.Status == "Deleted")
                return false;

            if (await _repo.VoucherCodeExistsAsync(dto.VoucherCode, id))
                throw new ArgumentException("Voucher code already exists.");
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Start date must be before end date.");
            if (dto.DiscountValue <= 0)
                throw new ArgumentException("Discount value must be positive.");

            existingVoucher.VoucherTypeId = dto.VoucherTypeId;
            existingVoucher.CreateBy = dto.CreateBy;
            existingVoucher.VoucherCode = dto.VoucherCode.Trim();
            existingVoucher.DiscountValue = dto.DiscountValue;
            existingVoucher.MaxDiscountAmount = dto.MaxDiscountAmount;
            existingVoucher.MinOrderAmount = dto.MinOrderAmount;
            existingVoucher.UsageLimitPerUser = dto.UsageLimitPerUser;
            existingVoucher.IsStackable = dto.IsStackable;
            existingVoucher.StartDate = dto.StartDate;
            existingVoucher.EndDate = dto.EndDate;
            existingVoucher.Status = dto.Status ?? "Active";
            existingVoucher.UpdatedAt = DateTime.Now;

            return await _repo.UpdateAsync(existingVoucher);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var voucher = await _repo.GetByIdAsync(id);
            if (voucher == null || voucher.Status == "Deleted")
                return false;

            voucher.Status = "Deleted";
            voucher.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(voucher);
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var voucher = await _repo.GetByIdAsync(id);
            if (voucher == null || voucher.Status != "Deleted")
                return false;

            voucher.Status = "Active";
            voucher.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(voucher);
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