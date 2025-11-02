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
    public class ReviewProductService : IReviewProductService
    {
        private readonly IReviewProductRepository _repo;

        public ReviewProductService(IReviewProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ReviewProductDto>> GetAllReviewsAsync(bool includeDeleted = false)
        {
            var reviews = await _repo.GetAllAsync();
            reviews = reviews.Where(r => r.Account?.Role?.RoleName == "Customer" && (includeDeleted || !r.IsDeleted)).ToList();
            return reviews.Select(r => MapToDto(r)).ToList();
        }

        public async Task<ReviewProductDto?> GetByIdAsync(int reviewProductId)
        {
            var review = await _repo.GetByIdAsync(reviewProductId);
            if (review == null || review.Account?.Role?.RoleName != "Customer")
                return null;
            return MapToDto(review);
        }

        public async Task<int> CreateAsync(ReviewProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Comment))
                throw new ArgumentException("Comment cannot be empty");

            if (!await CanReviewAsync(dto.ProductId, dto.AccountId))
                throw new InvalidOperationException("User has already reviewed this product.");

            var review = new ReviewProduct
            {
                ProductId = dto.ProductId,
                AccountId = dto.AccountId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                IsVerifiedPurchase = dto.IsVerifiedPurchase,
                CreatedAt = DateTime.Now
            };

            return await _repo.CreateAsync(review);
        }

        public async Task<bool> UpdateAsync(int id, ReviewProductDto dto)
        {
            var review = await _repo.GetByIdAsync(id);
            if (review == null || review.Account?.Role?.RoleName != "Customer")
                return false;

            review.IsVerifiedPurchase = dto.IsVerifiedPurchase;
            review.IsDeleted = dto.IsDeleted;
            review.UpdatedAt = DateTime.Now;

            return await _repo.UpdateAsync(review);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var review = await _repo.GetByIdAsync(id);
            if (review == null || review.Account?.Role?.RoleName != "Customer")
                return false;

            review.IsDeleted = true;
            review.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(review);
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var review = await _repo.GetByIdAsync(id);
            if (review == null || review.Account?.Role?.RoleName != "Customer" || !review.IsDeleted)
                return false;

            review.IsDeleted = false;
            review.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(review);
        }

        public async Task<bool> CanReviewAsync(int productId, int accountId)
        {
            return !await _repo.HasUserReviewedAsync(productId, accountId);
        }

        private ReviewProductDto MapToDto(ReviewProduct review)
        {
            var likeCount = review.ReviewProductReactions.Count(r => r.ReactionType == "Like");
            var dislikeCount = review.ReviewProductReactions.Count(r => r.ReactionType == "Dislike");

            return new ReviewProductDto
            {
                ReviewProductId = review.ReviewProductId,
                ProductId = review.ProductId,
                AccountId = review.AccountId,
                Rating = review.Rating,
                Comment = review.Comment,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                IsDeleted = review.IsDeleted,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                AuthorName = review.Account?.AccountName ?? "Anonymous",
                AuthorEmail = review.Account?.Email,
                ProductName = review.Product?.ProductName,
                LikeCount = likeCount,
                DislikeCount = dislikeCount,
                Replies = review.ReviewProductReplies
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new ReviewProductReplyDto
                    {
                        ReplyProductId = r.ReplyProductId,
                        ReviewProductId = r.ReviewProductId,
                        AccountId = r.AccountId,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        AuthorName = r.Account?.AccountName ?? "Anonymous",
                        IsAdmin = r.Account?.Role?.RoleName == "Admin" || r.Account?.Role?.RoleName == "Moderator"
                    }).ToList(),
                ImageUrls = review.ReviewProductImages
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.ImageUrl)
                    .ToList()
            };
        }

        public async Task<ReviewProductDto?> GetReviewByProductAndAccountAsync(int productId, int accountId)
        {
            var reviews = await _repo.GetAllAsync();
            var review = reviews
                .FirstOrDefault(r => r.ProductId == productId && r.AccountId == accountId && !r.IsDeleted);

            return review != null ? MapToDto(review) : null;
        }

        public async Task<List<ReviewProductDto>> GetReviewsByProductIdAsync(int productId)
        {
            var reviews = await _repo.GetAllAsync();
            return reviews
                .Where(r => r.ProductId == productId && !r.IsDeleted && r.Account?.Role?.RoleName == "Customer")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => MapToDto(r))
                .ToList();
        }

        public async Task<List<ReviewProductDto>> GetReviewsByAccountAsync(int accountId)
        {
            var reviews = await _repo.GetAllAsync();

            return reviews
                .Where(r => r.AccountId == accountId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => MapToDto(r))
                .ToList();
        }

        public async Task<bool> HasUserReviewedAsync(int productId, int accountId)
        {
            return await _repo.HasUserReviewedAsync(productId, accountId);
        }

    }
}