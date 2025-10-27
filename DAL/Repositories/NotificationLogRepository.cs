// DAL/Repositories/NotificationLogRepository.cs
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class NotificationLogRepository : INotificationLogRepository
    {
        private readonly AspLorKingDomContext _db;
        public NotificationLogRepository(AspLorKingDomContext db) => _db = db;

        public async Task AddAsync(NotificationLog log)
        {
            _db.NotificationLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<IList<NotificationLog>> GetByNotificationIdAsync(int notificationId, int take = 200)
        {
            if (take <= 0) take = 50;
            if (take > 1000) take = 1000;

            return await _db.NotificationLogs
                .AsNoTracking()
                .Where(l => l.NotificationId == notificationId)
                .OrderByDescending(l => l.SentAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
