using DAL.Models;
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

        Task MarkReadAsync(int userNotificationId, System.DateTime readAtUtc);

        Task MarkDeliveredAsync(int userNotificationId, System.DateTime deliveredAtUtc);
    }
}
