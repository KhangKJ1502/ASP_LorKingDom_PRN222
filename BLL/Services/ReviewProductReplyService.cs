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
    public class ReviewProductReplyService : IReviewProductReplyService
    {
        private readonly IReviewProductReplyRepository _repo;

        public ReviewProductReplyService(IReviewProductReplyRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ReviewProductReplyDto>> GetByReviewIdAsync(int reviewProductId)
        {
            var replies = await _repo.GetByReviewIdAsync(reviewProductId);
            return replies.Select(r => new ReviewProductReplyDto
            {
                ReplyProductId = r.ReplyProductId,
                ReviewProductId = r.ReviewProductId,
                AccountId = r.AccountId,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                AuthorName = r.Account?.AccountName ?? "Anonymous",
                IsAdmin = r.Account?.Role?.RoleName == "Admin" || r.Account?.Role?.RoleName == "Moderator"
            }).ToList();
        }

        public async Task<ReviewProductReplyDto?> GetByIdAsync(int replyId)
        {
            var reply = await _repo.GetByIdAsync(replyId);
            if (reply == null) return null;

            return new ReviewProductReplyDto
            {
                ReplyProductId = reply.ReplyProductId,
                ReviewProductId = reply.ReviewProductId,
                AccountId = reply.AccountId,
                Content = reply.Content,
                CreatedAt = reply.CreatedAt,
                AuthorName = reply.Account?.AccountName ?? "Anonymous",
                IsAdmin = reply.Account?.Role?.RoleName == "Admin" || reply.Account?.Role?.RoleName == "Moderator"
            };
        }

        public async Task<int> CreateAsync(ReviewProductReplyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Reply content cannot be empty");

            var reply = new ReviewProductReply
            {
                ReviewProductId = dto.ReviewProductId,
                AccountId = dto.AccountId,
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.Now
            };

            return await _repo.CreateAsync(reply);
        }

        public async Task<bool> DeleteAsync(int replyId)
        {
            return await _repo.DeleteAsync(replyId);
        }
    }
}