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
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Tên khuyến mãi không được để trống.");
            if (dto.EndDate < dto.StartDate) throw new ArgumentException("Ngày kết thúc phải >= ngày bắt đầu.");
            if (dto.DiscountPercent is < 0m or > 100m) throw new ArgumentException("Phần trăm giảm phải trong khoảng 0–100.");

            if (await _repo.ExistsByNameAsync(dto.Name))
                throw new InvalidOperationException("Tên khuyến mãi đã tồn tại.");

            if (dto.ProductId.HasValue)
            {
                var overlap = await _repo.HasOverlapAsync(dto.ProductId.Value, dto.StartDate, dto.EndDate, excludeId: null);
                if (overlap) throw new InvalidOperationException("Khoảng thời gian khuyến mãi trùng với khuyến mãi khác của sản phẩm này.");
            }
        }

        public async Task ThrowIfInvalidUpdateAsync(PromotionUpdateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.PromotionId <= 0) throw new ArgumentException("PromotionId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Tên khuyến mãi không được để trống.");
            if (dto.EndDate < dto.StartDate) throw new ArgumentException("Ngày kết thúc phải >= ngày bắt đầu.");
            if (dto.DiscountPercent is < 0m or > 100m) throw new ArgumentException("Phần trăm giảm phải trong khoảng 0–100.");

            if (await _repo.ExistsByNameAsync(dto.Name, excludeId: dto.PromotionId))
                throw new InvalidOperationException("Tên khuyến mãi đã tồn tại.");

            if (dto.ProductId.HasValue)
            {
                var overlap = await _repo.HasOverlapAsync(dto.ProductId.Value, dto.StartDate, dto.EndDate, excludeId: dto.PromotionId);
                if (overlap) throw new InvalidOperationException("Khoảng thời gian khuyến mãi trùng với khuyến mãi khác của sản phẩm này.");
            }
        }
    }
}
