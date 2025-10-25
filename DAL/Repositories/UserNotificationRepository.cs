using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class UserNotificationRepository : IUserNotificationRepository
    {
        private readonly AspLorKingDomContext _db;
        public UserNotificationRepository(AspLorKingDomContext db) => _db = db;

        public async Task CreateRangeAsync(IEnumerable<UserNotification> items)
        {
            await _db.UserNotifications.AddRangeAsync(items);
            await _db.SaveChangesAsync();
        }

        public async Task<(IList<UserNotification> Items, int Total)> GetByUserAsync(
            int userId, bool? isRead, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            IQueryable<UserNotification> q = _db.UserNotifications
                .AsNoTracking()
                .Include(x => x.Notification)
                .Where(x => x.UserId == userId);

            if (isRead.HasValue)
                q = q.Where(x => x.IsRead == isRead.Value);

            q = q.OrderByDescending(x => x.DeliveredAt ?? x.Notification!.ScheduledAt);

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }

        public Task<UserNotification?> GetByIdAsync(int userNotificationId)
        {
            return _db.UserNotifications
                .Include(x => x.Notification)
                .FirstOrDefaultAsync(x => x.UserNotificationId == userNotificationId);
        }

        // NEW
        public Task<UserNotification?> GetByIdForUserAsync(int userNotificationId, int userId)
        {
            return _db.UserNotifications
                .Include(x => x.Notification)
                .FirstOrDefaultAsync(x =>
                    x.UserNotificationId == userNotificationId && x.UserId == userId);
        }

        public async Task MarkReadAsync(int userNotificationId, DateTime readAtUtc)
        {
            var un = await _db.UserNotifications.FindAsync(userNotificationId);
            if (un == null) return;

            if (!un.IsRead)
            {
                un.IsRead = true;
                un.ReadAt = readAtUtc;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkDeliveredAsync(int userNotificationId, DateTime deliveredAtUtc)
        {
            var un = await _db.UserNotifications.FindAsync(userNotificationId);
            if (un == null) return;

            un.DeliveredAt = deliveredAtUtc;
            await _db.SaveChangesAsync();
        }

        // NEW
        public async Task<int> MarkAllReadAsync(int userId, DateTime readAtUtc)
        {
            var list = await _db.UserNotifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync();

            if (list.Count == 0) return 0;

            foreach (var it in list)
            {
                it.IsRead = true;
                it.ReadAt = readAtUtc;
            }

            await _db.SaveChangesAsync();
            return list.Count;
        }

        // NEW
        public Task<int> GetUnreadCountAsync(int userId)
        {
            return _db.UserNotifications
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.IsRead)
                .CountAsync();
        }
    }
}
