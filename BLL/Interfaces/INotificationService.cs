using BLL.DTOs;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface INotificationService
    {
        // ADMIN + QUERY
        Task<PagedResult<NotificationDto>> SearchAsync(NotificationFilterDto f);
        Task<NotificationDto?> GetByIdAsync(int id);

        Task<NotificationDto> CreateAsync(NotificationCreateDto dto);
        Task UpdateAsync(NotificationUpdateDto dto);
        Task<bool> DeleteAsync(int id);

        Task CancelAsync(int id);
        Task SendNowAsync(int id);

        // WORKER
        Task<int> DispatchDueAsync();
    }
}
