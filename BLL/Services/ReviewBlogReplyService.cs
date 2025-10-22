using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class ReviewBlogReplyService : IReviewBlogReplyService
    {
        private readonly IReviewBlogReplyRepository _repo;

        public ReviewBlogReplyService(IReviewBlogReplyRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ReviewBlogReplyDto>> GetByReviewIdAsync(int reviewBlogId)
        {
            var replies = await _repo.GetByReviewIdAsync(reviewBlogId);
            return replies.Select(r => new ReviewBlogReplyDto
            {
                ReplyBlogId = r.ReplyBlogId,
                ReviewBlogId = r.ReviewBlogId,
                AccountId = r.AccountId,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                AuthorName = r.Account?.AccountName ?? "Anonymous",
                AuthorImage = r.Account?.Image,
                IsAdmin = r.Account?.Role?.RoleName == "Admin" || r.Account?.Role?.RoleName == "Moderator"
            }).ToList();
        }

        public async Task<ReviewBlogReplyDto?> GetByIdAsync(int replyId)
        {
            var reply = await _repo.GetByIdAsync(replyId);
            if (reply == null) return null;

            return new ReviewBlogReplyDto
            {
                ReplyBlogId = reply.ReplyBlogId,
                ReviewBlogId = reply.ReviewBlogId,
                AccountId = reply.AccountId,
                Content = reply.Content,
                CreatedAt = reply.CreatedAt,
                AuthorName = reply.Account?.AccountName ?? "Anonymous",
                AuthorImage = reply.Account?.Image,
                IsAdmin = reply.Account?.Role?.RoleName == "Admin" || reply.Account?.Role?.RoleName == "Moderator"
            };
        }

        public async Task<int> CreateAsync(ReviewBlogReplyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Nội dung phản hồi không được để trống");

            var reply = new ReviewBlogReply
            {
                ReviewBlogId = dto.ReviewBlogId,
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
