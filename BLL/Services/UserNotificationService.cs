// BLL/Services/UserNotificationService.cs
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    /// <summary>
    /// Service cho người dùng cuối xem / đánh dấu thông báo của họ.
    /// </summary>
    public class UserNotificationService : IUserNotificationService
    {
        private readonly IUserNotificationRepository _repo;

        public UserNotificationService(IUserNotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<UserNotificationDto>> GetMyNotificationsAsync(int userId, bool? isRead, int page, int pageSize)
        {
            var (items, total) = await _repo.GetByUserAsync(userId, isRead, page, pageSize);

            return new PagedResult<UserNotificationDto>
            {
                Items = items.Select(x => new UserNotificationDto
                {
                    UserNotificationId = x.UserNotificationId,
                    NotificationId = x.NotificationId,
                    UserId = x.UserId,
                    IsRead = x.IsRead,
                    ReadAt = x.ReadAt,
                    DeliveredAt = x.DeliveredAt,
                    Title = x.Notification?.Title ?? "",
                    Message = x.Notification?.Message ?? "",
                    Type = x.Notification?.Type ?? "General",
                    ScheduledAt = x.Notification?.ScheduledAt ?? DateTime.MinValue
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task MarkReadAsync(int userNotificationId)
        {
            // gọi thẳng repo.MarkReadAsync().
            // QUAN TRỌNG: controller sẽ check quyền sở hữu trước khi gọi chứ service này không check userId.
            await _repo.MarkReadAsync(userNotificationId, DateTime.UtcNow);
        }

        public async Task<UserNotificationDto?> GetUserNotificationByIdAsync(int userNotificationId)
        {
            var e = await _repo.GetByIdAsync(userNotificationId);
            return e == null ? null : new UserNotificationDto
            {
                UserNotificationId = e.UserNotificationId,
                NotificationId = e.NotificationId,
                UserId = e.UserId,
                IsRead = e.IsRead,
                ReadAt = e.ReadAt,
                DeliveredAt = e.DeliveredAt,
                Title = e.Notification?.Title ?? "",
                Message = e.Notification?.Message ?? "",
                Type = e.Notification?.Type ?? "General",
                ScheduledAt = e.Notification?.ScheduledAt ?? DateTime.MinValue
            };
        }

        public Task<int> MarkAllReadAsync(int userId)
            => _repo.MarkAllReadAsync(userId, DateTime.UtcNow);

        public Task<int> GetUnreadCountAsync(int userId)
            => _repo.GetUnreadCountAsync(userId);
    }
}
