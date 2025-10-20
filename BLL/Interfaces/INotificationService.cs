using BLL.DTOs;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationDto>> SearchAsync(NotificationFilterDto f);
        Task<NotificationDto?> GetByIdAsync(int id);
        Task<NotificationDto> CreateAsync(NotificationCreateDto dto);
        Task UpdateAsync(NotificationUpdateDto dto);
        Task DeleteAsync(int id);

        Task CancelAsync(int id);
        Task SendNowAsync(int id); // cưỡng bức gửi ngay

        // User side
        Task<PagedResult<UserNotificationDto>> GetMyNotificationsAsync(int userId, bool? isRead, int page, int pageSize);
        Task MarkReadAsync(int userNotificationId);

        // Worker
        Task<int> DispatchDueAsync();
    }
}
