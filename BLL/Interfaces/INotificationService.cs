using BLL.DTOs;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    /// <summary>
    /// Service cho quản trị & worker xử lý thông báo (CRUD, gửi, hủy, dispatch).
    /// </summary>
    public interface INotificationService
    {
        // Admin
        Task<PagedResult<NotificationDto>> SearchAsync(NotificationFilterDto f);
        Task<NotificationDto?> GetByIdAsync(int id);
        Task<NotificationDto> CreateAsync(NotificationCreateDto dto);
        Task UpdateAsync(NotificationUpdateDto dto);
        Task DeleteAsync(int id);

        Task CancelAsync(int id);
        Task SendNowAsync(int id);

        // Worker
        Task<int> DispatchDueAsync();
    }
}
