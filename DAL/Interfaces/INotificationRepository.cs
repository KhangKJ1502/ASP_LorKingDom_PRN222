using DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface INotificationRepository
    {
        Task<(IList<Notification> Items, int Total)> SearchAsync(
            string? keyword,
            string? type,
            string? targetType,
            int? targetRoleId,
            int? targetUserId,
            bool? isSent,
            bool? isCanceled,
            DateTime? scheduledFrom,
            DateTime? scheduledTo,
            int page = 1,
            int pageSize = 20);

        Task<Notification?> GetByIdAsync(int id);

        Task<bool> ExistsByTitleAsync(string title, int? excludeId = null);

        Task<Notification> CreateAsync(Notification entity);
        Task UpdateAsync(Notification entity);
        Task DeleteAsync(int id);

        // Worker/scheduler
        Task<IList<Notification>> GetDuePendingAsync(DateTime nowUtc, int take = 100);
        Task MarkSentAsync(int id, DateTime sentAtUtc);
        Task CancelAsync(int id);
    }
}
