using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AspLorKingDomContext _db;

        public NotificationRepository(AspLorKingDomContext db) => _db = db;

        public async Task<(IList<Notification> Items, int Total)> SearchAsync(
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
            int pageSize = 20)
        {
            // clamp paging
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            IQueryable<Notification> q = _db.Notifications.AsNoTracking();

            // keyword on Title or Message (use LIKE)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = $"%{keyword.Trim()}%";
                q = q.Where(x =>
                    EF.Functions.Like(x.Title, kw) ||
                    EF.Functions.Like(x.Message, kw));
            }

            // exact filters
            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(x => x.Type == type); // 'General','Order','Promotion','System'

            if (!string.IsNullOrWhiteSpace(targetType))
                q = q.Where(x => x.TargetType == targetType); // 'All','SingleUser','ByRole','ByCondition'

            if (targetRoleId.HasValue)
                q = q.Where(x => x.TargetRoleId == targetRoleId.Value);

            if (targetUserId.HasValue)
                q = q.Where(x => x.TargetUserId == targetUserId.Value);

            if (isSent.HasValue)
                q = q.Where(x => x.IsSent == isSent.Value);

            if (isCanceled.HasValue)
                q = q.Where(x => x.IsCanceled == isCanceled.Value);

            if (scheduledFrom.HasValue)
                q = q.Where(x => x.ScheduledAt >= scheduledFrom.Value);

            if (scheduledTo.HasValue)
                q = q.Where(x => x.ScheduledAt <= scheduledTo.Value);

            // newest first by ScheduledAt
            q = q.OrderByDescending(x => x.ScheduledAt);

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<Notification?> GetByIdAsync(int id)
        {
            // thường cần Role/User/Creator để hiển thị UI
            return _db.Notifications
                .AsSplitQuery()
                .Include(x => x.TargetRole)
                .Include(x => x.TargetUser)
                .Include(x => x.CreatedByNavigation)
                .FirstOrDefaultAsync(x => x.NotificationId == id);
        }

        public async Task<bool> ExistsByTitleAsync(string title, int? excludeId = null)
        {
            var q = _db.Notifications.AsNoTracking().Where(x => x.Title == title);
            if (excludeId.HasValue)
                q = q.Where(x => x.NotificationId != excludeId.Value);
            return await q.AnyAsync();
        }

        public async Task<Notification> CreateAsync(Notification entity)
        {
            _db.Notifications.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(Notification entity)
        {
            // Có thể dùng attach + set modified selective nếu cần concurrency
            _db.Notifications.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Notifications.FindAsync(id);
            if (entity == null) return;

            _db.Notifications.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task<IList<Notification>> GetDuePendingAsync(DateTime nowUtc, int take = 100)
        {
            if (take <= 0) take = 100;
            if (take > 500) take = 500;

            return await _db.Notifications
                .AsNoTracking()
                .Where(x =>
                    !x.IsSent &&
                    !x.IsCanceled &&
                    x.ScheduledAt <= nowUtc &&
                    (x.ExpireAt == null || x.ExpireAt > nowUtc))
                .OrderBy(x => x.ScheduledAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task MarkSentAsync(int id, DateTime sentAtUtc)
        {
            var entity = await _db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == id);
            if (entity == null) return;

            if (!entity.IsSent)
            {
                entity.IsSent = true;
                entity.SentAt = sentAtUtc;
                await _db.SaveChangesAsync();
            }
        }

        public async Task CancelAsync(int id)
        {
            var entity = await _db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == id);
            if (entity == null) return;

            if (!entity.IsCanceled)
            {
                entity.IsCanceled = true;
                await _db.SaveChangesAsync();
            }
        }
    }
}
