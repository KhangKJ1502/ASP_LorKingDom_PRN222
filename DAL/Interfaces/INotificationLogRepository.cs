using DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface INotificationLogRepository
    {
        Task AddAsync(NotificationLog log);

        Task<IList<NotificationLog>> GetByNotificationIdAsync(int notificationId, int take = 200);
    }
}
