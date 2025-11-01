// BLL/Validators/PromotionValidator.cs
using BLL.DTOs;
using DAL.Interfaces;
using System;
using System.Threading.Tasks;

namespace BLL.Validators
{
    public class PromotionValidator
    {
        private readonly IPromotionRepository _repo;

        public PromotionValidator(IPromotionRepository repo)
        {
            _repo = repo;
        }

        public async Task ThrowIfInvalidCreateAsync(PromotionCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.PromotionCode))
                throw new ArgumentException("Mã khuyến mãi không được để trống.");

            if (dto.EndDate < dto.StartDate)
                throw new ArgumentException("Ngày kết thúc phải >= ngày bắt đầu.");

            if (dto.DiscountPercent is < 0m or > 100m)
                throw new ArgumentException("Phần trăm giảm phải trong khoảng 0–100.");

            if (await _repo.ExistsByNameAsync(dto.PromotionCode))
                throw new InvalidOperationException("Mã khuyến mãi đã tồn tại.");

            // OPTIONAL: Bỏ comment dòng dưới nếu muốn cho phép nhiều promotion cùng thời gian
            // var overlap = await _repo.HasOverlapAsync(0, dto.StartDate, dto.EndDate, excludeId: null);
            // if (overlap)
            //     throw new InvalidOperationException("Khoảng thời gian khuyến mãi trùng với khuyến mãi khác.");
        }

        public async Task ThrowIfInvalidUpdateAsync(PromotionUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.PromotionId <= 0)
                throw new ArgumentException("PromotionId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.PromotionCode))
                throw new ArgumentException("Mã khuyến mãi không được để trống.");

            if (dto.EndDate < dto.StartDate)
                throw new ArgumentException("Ngày kết thúc phải >= ngày bắt đầu.");

            if (dto.DiscountPercent is < 0m or > 100m)
                throw new ArgumentException("Phần trăm giảm phải trong khoảng 0–100.");

            if (await _repo.ExistsByNameAsync(dto.PromotionCode, excludeId: dto.PromotionId))
                throw new InvalidOperationException("Mã khuyến mãi đã tồn tại (trùng với khuyến mãi khác).");

            // OPTIONAL: Bỏ comment dòng dưới nếu muốn cho phép nhiều promotion cùng thời gian
            // var overlap = await _repo.HasOverlapAsync(0, dto.StartDate, dto.EndDate, excludeId: dto.PromotionId);
            // if (overlap)
            //     throw new InvalidOperationException($"Khoảng thời gian {dto.StartDate:dd/MM/yyyy} - {dto.EndDate:dd/MM/yyyy} trùng với khuyến mãi khác trong hệ thống.");
        }
    }
}