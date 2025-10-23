using BLL.DTOs;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    /// <summary>
    /// Service cho tính năng thông báo phía người dùng cuối.
    /// </summary>
    public interface IUserNotificationService
    {
        Task<PagedResult<UserNotificationDto>> GetMyNotificationsAsync(int userId, bool? isRead, int page, int pageSize);
        Task MarkReadAsync(int userNotificationId);
        Task<UserNotificationDto?> GetUserNotificationByIdAsync(int userNotificationId);
        Task<int> MarkAllReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
