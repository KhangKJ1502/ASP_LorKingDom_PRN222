using DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IUserNotificationRepository
    {
        Task CreateRangeAsync(IEnumerable<UserNotification> items);

        Task<(IList<UserNotification> Items, int Total)> GetByUserAsync(
            int userId, bool? isRead, int page = 1, int pageSize = 20);

        Task<UserNotification?> GetByIdAsync(int userNotificationId);

        // NEW: lấy 1 bản ghi kèm kiểm tra thuộc về user
        Task<UserNotification?> GetByIdForUserAsync(int userNotificationId, int userId);

        Task MarkReadAsync(int userNotificationId, DateTime readAtUtc);

        Task MarkDeliveredAsync(int userNotificationId, DateTime deliveredAtUtc);

        // NEW: batch cập nhật tất cả chưa đọc -> đã đọc
        Task<int> MarkAllReadAsync(int userId, DateTime readAtUtc);

        // NEW: đếm nhanh số chưa đọc
        Task<int> GetUnreadCountAsync(int userId);
    }
}
