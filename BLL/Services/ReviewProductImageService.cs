using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ReviewProductImageService : IReviewProductImageService
    {
        private readonly IReviewProductImageRepository _repo;

        public ReviewProductImageService(IReviewProductImageRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> CreateAsync(ReviewProductImageDto dto)
        {
            var image = new ReviewProductImage
            {
                ReviewProductId = dto.ReviewProductId,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            return await _repo.CreateAsync(image);
        }

        public async Task<bool> DeleteAsync(int imageId)
        {
            return await _repo.DeleteAsync(imageId);
        }
    }
}