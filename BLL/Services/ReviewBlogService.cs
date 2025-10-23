using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class ReviewBlogService : IReviewBlogService
    {
        private readonly IReviewBlogRepository _repo;

        public ReviewBlogService(IReviewBlogRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ReviewBlogDto>> GetByBlogIdAsync(int blogPostId, int? currentUserId = null)
        {
            var reviews = await _repo.GetByBlogIdAsync(blogPostId);
            // Lọc bỏ các bình luận bị cấm
            var activeReviews = reviews.Where(r => !r.IsBlocked).ToList();
            return activeReviews.Select(r => MapToDto(r, currentUserId)).ToList();
        }

        public async Task<List<ReviewBlogDto>> GetAllReviewsAsync()
        {
            var reviews = await _repo.GetAllAsync();
            return reviews.Select(r => MapToDto(r, null)).ToList();
        }

        public async Task<ReviewBlogDto?> GetByIdAsync(int reviewBlogId, int? currentUserId = null)
        {
            var review = await _repo.GetByIdAsync(reviewBlogId);
            return review == null ? null : MapToDto(review, currentUserId);
        }

        public async Task<int> CreateAsync(ReviewBlogDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Comment))
                throw new ArgumentException("Bình luận không được để trống");

            // Check if user already commented
            if (!await CanCommentAsync(dto.BlogPostId, dto.AccountId))
                throw new InvalidOperationException("Bạn đã bình luận rồi. Mỗi người chỉ được bình luận 1 lần trên mỗi bài viết.");

            var review = new ReviewBlog
            {
                BlogPostId = dto.BlogPostId,
                AccountId = dto.AccountId,
                Rating = dto.Rating,
                Comment = dto.Comment.Trim(),
                CreatedAt = DateTime.Now
            };

            return await _repo.CreateAsync(review);
        }

        public async Task<bool> UpdateAsync(int id, ReviewBlogDto dto)
        {
            var review = await _repo.GetByIdAsync(id);
            if (review == null) return false;

            review.Comment = dto.Comment?.Trim() ?? review.Comment;
            review.Rating = dto.Rating;
            review.IsBlocked = dto.IsBlocked;

            return await _repo.UpdateAsync(review);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<bool> CanCommentAsync(int blogPostId, int accountId)
        {
            return !await _repo.HasUserCommentedAsync(blogPostId, accountId);
        }

        private ReviewBlogDto MapToDto(ReviewBlog review, int? currentUserId)
        {
            var likeCount = review.ReviewBlogReactions.Count(r => r.ReactionType == "like");
            var dislikeCount = review.ReviewBlogReactions.Count(r => r.ReactionType == "dislike");

            var currentUserReaction = currentUserId.HasValue
                ? review.ReviewBlogReactions.FirstOrDefault(r => r.AccountId == currentUserId.Value)
                : null;

            bool? userReactionType = null;
            if (currentUserReaction != null)
            {
                userReactionType = currentUserReaction.ReactionType == "like";
            }

            return new ReviewBlogDto
            {
                ReviewBlogId = review.ReviewBlogId,
                BlogPostId = review.BlogPostId,
                AccountId = review.AccountId,
                Rating = review.Rating,
                Comment = review.Comment,
                IsBlocked = review.IsBlocked,
                CreatedAt = review.CreatedAt,
                AuthorName = review.Account?.AccountName ?? "Anonymous",
                AuthorEmail = review.Account?.Email,
                AuthorImage = review.Account?.Image,
                LikeCount = likeCount,
                DislikeCount = dislikeCount,
                CurrentUserReactionType = userReactionType,
                Replies = review.ReviewBlogReplies
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new ReviewBlogReplyDto
                    {
                        ReplyBlogId = r.ReplyBlogId,
                        ReviewBlogId = r.ReviewBlogId,
                        AccountId = r.AccountId,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        AuthorName = r.Account?.AccountName ?? "Anonymous",
                        AuthorImage = r.Account?.Image,
                        IsAdmin = r.Account?.Role?.RoleName == "Admin" || r.Account?.Role?.RoleName == "Moderator"
                    })
                    .ToList()
            };
        }
    }
}
