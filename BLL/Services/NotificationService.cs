using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserNotificationRepository _userNotificationRepo;
        private readonly INotificationLogRepository _logRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IRoleRepository _roleRepo;

        public NotificationService(
            INotificationRepository notificationRepo,
            IUserNotificationRepository userNotificationRepo,
            INotificationLogRepository logRepo,
            IAccountRepository accountRepo,
            IRoleRepository roleRepo)
        {
            _notificationRepo = notificationRepo;
            _userNotificationRepo = userNotificationRepo;
            _logRepo = logRepo;
            _accountRepo = accountRepo;
            _roleRepo = roleRepo;
        }

        // =================== Admin ===================

        public async Task<PagedResult<NotificationDto>> SearchAsync(NotificationFilterDto f)
        {
            var (items, total) = await _notificationRepo.SearchAsync(
                f.Keyword, f.Type, f.TargetType, f.TargetRoleId, f.TargetUserId,
                f.IsSent, f.IsCanceled, f.ScheduledFrom, f.ScheduledTo,
                f.Page, f.PageSize);

            return new PagedResult<NotificationDto>
            {
                Items = items.Select(MapToDto).ToList(),
                Total = total,
                Page = f.Page,
                PageSize = f.PageSize
            };
        }

        public async Task<NotificationDto?> GetByIdAsync(int id)
        {
            var entity = await _notificationRepo.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<NotificationDto> CreateAsync(NotificationCreateDto dto)
        {
            await ValidateCreateAsync(dto);

            var entity = new Notification
            {
                Title = dto.Title.Trim(),
                Message = dto.Message.Trim(),
                Type = NormalizeType(dto.Type),
                TargetType = NormalizeTargetType(dto.TargetType),
                TargetRoleId = dto.TargetRoleId,
                TargetUserId = dto.TargetUserId,
                ConditionJson = dto.ConditionJson,
                ScheduledAt = EnsureUtc(dto.ScheduledAt),
                ExpireAt = dto.ExpireAt.HasValue ? EnsureUtc(dto.ExpireAt.Value) : null,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                IsSent = false,
                IsCanceled = false
            };

            var created = await _notificationRepo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task UpdateAsync(NotificationUpdateDto dto)
        {
            var entity = await _notificationRepo.GetByIdAsync(dto.NotificationId)
                         ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID {dto.NotificationId}");

            if (entity.IsSent) throw new InvalidOperationException("Không thể sửa thông báo đã gửi");
            if (entity.IsCanceled) throw new InvalidOperationException("Không thể sửa thông báo đã hủy");

            // Nếu đổi title thì kiểm tra trùng
            if (!string.Equals(entity.Title, dto.Title.Trim(), StringComparison.Ordinal))
            {
                var duplicated = await _notificationRepo.ExistsByTitleAsync(dto.Title.Trim(), dto.NotificationId);
                if (duplicated) throw new InvalidOperationException($"Tiêu đề '{dto.Title}' đã tồn tại");
            }

            // Chỉ cho phép sửa các field nội dung & thời gian (tránh đổi đối tượng nhận sau khi đã lên lịch)
            entity.Title = dto.Title.Trim();
            entity.Message = dto.Message.Trim();
            entity.Type = NormalizeType(dto.Type);
            entity.ScheduledAt = EnsureUtc(dto.ScheduledAt);
            entity.ExpireAt = dto.ExpireAt.HasValue ? EnsureUtc(dto.ExpireAt.Value) : null;

            await _notificationRepo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _notificationRepo.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID {id}");
            if (entity.IsSent) throw new InvalidOperationException("Không thể xóa thông báo đã gửi");

            await _notificationRepo.DeleteAsync(id);
        }

        public async Task CancelAsync(int id)
        {
            var entity = await _notificationRepo.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID {id}");

            if (entity.IsSent) throw new InvalidOperationException("Không thể hủy thông báo đã gửi");
            if (entity.IsCanceled) throw new InvalidOperationException("Thông báo đã được hủy trước đó");

            await _notificationRepo.CancelAsync(id);

            await _logRepo.AddAsync(new NotificationLog
            {
                NotificationId = id,
                Result = "Canceled",
                Details = "Thông báo bị hủy bởi admin",
                SentAt = DateTime.UtcNow
            });
        }

        public async Task SendNowAsync(int id)
        {
            var notif = await _notificationRepo.GetByIdAsync(id)
                        ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID {id}");
            if (notif.IsCanceled) throw new InvalidOperationException("Thông báo đã bị hủy");
            if (notif.IsSent) throw new InvalidOperationException("Thông báo đã gửi trước đó");

            // Cưỡng bức gửi ngay
            await DispatchOneAsync(notif, DateTime.UtcNow);
        }

        // =================== User ===================

        public async Task<PagedResult<UserNotificationDto>> GetMyNotificationsAsync(int userId, bool? isRead, int page, int pageSize)
        {
            var (items, total) = await _userNotificationRepo.GetByUserAsync(userId, isRead, page, pageSize);

            var dtos = items.Select(x => new UserNotificationDto
            {
                UserNotificationId = x.UserNotificationId,
                NotificationId = x.NotificationId,
                UserId = x.UserId,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt,
                DeliveredAt = x.DeliveredAt,
                Title = x.Notification?.Title ?? "",
                Message = x.Notification?.Message ?? "",
                ScheduledAt = x.Notification?.ScheduledAt ?? DateTime.MinValue
            }).ToList();

            return new PagedResult<UserNotificationDto>
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task MarkReadAsync(int userNotificationId)
        {
            // Không nhận userId ở chữ ký -> chỉ đánh dấu read, phần bảo mật nên kiểm ở Controller bằng user hiện tại
            await _userNotificationRepo.MarkReadAsync(userNotificationId, DateTime.UtcNow);
        }

        // =================== Worker ===================

        public async Task<int> DispatchDueAsync()
        {
            var now = DateTime.UtcNow;
            var due = await _notificationRepo.GetDuePendingAsync(now, 200);
            int success = 0;

            foreach (var notif in due)
            {
                var ok = await DispatchOneAsync(notif, now);
                if (ok) success++;
            }
            return success;
        }

        // =================== Private helpers ===================

        private NotificationDto MapToDto(Notification e)
        {
            return new NotificationDto
            {
                NotificationId = e.NotificationId,
                TargetRoleId = e.TargetRoleId,
                TargetUserId = e.TargetUserId,
                CreatedBy = e.CreatedBy,
                ConditionJson = e.ConditionJson,
                Title = e.Title,
                Message = e.Message,
                Type = e.Type,
                TargetType = e.TargetType,
                ScheduledAt = e.ScheduledAt,
                SentAt = e.SentAt,
                ExpireAt = e.ExpireAt,
                IsSent = e.IsSent,
                IsCanceled = e.IsCanceled,
                CreatedAt = e.CreatedAt
            };
        }

        private async Task ValidateCreateAsync(NotificationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Title không được rỗng");

            if (await _notificationRepo.ExistsByTitleAsync(dto.Title.Trim()))
                throw new InvalidOperationException($"Tiêu đề '{dto.Title}' đã tồn tại");

            var tt = NormalizeTargetType(dto.TargetType);
            switch (tt)
            {
                case "SingleUser":
                    if (!dto.TargetUserId.HasValue)
                        throw new InvalidOperationException("Phải chỉ định TargetUserId khi TargetType = SingleUser");
                    break;
                case "ByRole":
                    if (!dto.TargetRoleId.HasValue)
                        throw new InvalidOperationException("Phải chỉ định TargetRoleId khi TargetType = ByRole");
                    break;
                case "ByCondition":
                    if (string.IsNullOrWhiteSpace(dto.ConditionJson))
                        throw new InvalidOperationException("Phải chỉ định ConditionJson khi TargetType = ByCondition");
                    break;
            }

            var now = DateTime.UtcNow.AddMinutes(-5);
            if (EnsureUtc(dto.ScheduledAt) < now)
                throw new InvalidOperationException("Thời gian gửi không được ở quá khứ");

            if (dto.ExpireAt.HasValue && EnsureUtc(dto.ExpireAt.Value) <= EnsureUtc(dto.ScheduledAt))
                throw new InvalidOperationException("ExpireAt phải sau ScheduledAt");
        }

        private static DateTime EnsureUtc(DateTime dt)
            => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        private static string NormalizeType(string type)
        {
            // Chuẩn hóa theo schema DB: General|Order|Promotion|System
            var t = (type ?? "").Trim().ToLowerInvariant();
            return t switch
            {
                "general" => "General",
                "order" => "Order",
                "promotion" => "Promotion",
                "system" => "System",
                _ => "General"
            };
        }

        private static string NormalizeTargetType(string targetType)
        {
            // Chuẩn hóa: All|SingleUser|ByRole|ByCondition
            var t = (targetType ?? "").Trim().ToLowerInvariant();
            return t switch
            {
                "all" => "All",
                "singleuser" or "user" => "SingleUser",
                "byrole" or "role" => "ByRole",
                "bycondition" or "condition" => "ByCondition",
                _ => "All"
            };
        }

        private async Task<bool> DispatchOneAsync(Notification notif, DateTime nowUtc)
        {
            try
            {
                // Bỏ qua nếu đã hết hạn tại thời điểm gửi
                if (notif.ExpireAt.HasValue && notif.ExpireAt.Value <= nowUtc)
                {
                    await _logRepo.AddAsync(new NotificationLog
                    {
                        NotificationId = notif.NotificationId,
                        SentTo = GetSentToDescription(notif),
                        Result = "Skipped",
                        Details = "Hết hạn trước khi gửi",
                        SentAt = nowUtc
                    });
                    return false;
                }

                var recipients = await BuildRecipientsAsync(notif);

                if (recipients.Count == 0)
                {
                    await _logRepo.AddAsync(new NotificationLog
                    {
                        NotificationId = notif.NotificationId,
                        SentTo = GetSentToDescription(notif),
                        Result = "Skipped",
                        Details = "Không có người nhận",
                        SentAt = nowUtc
                    });
                    // Vẫn mark sent để không lặp vô hạn
                    await _notificationRepo.MarkSentAsync(notif.NotificationId, nowUtc);
                    return true;
                }

                // Tạo bản ghi UserNotifications (đánh dấu DeliveredAt để hiển thị lên đầu)
                var userNotifs = recipients.Select(uid => new UserNotification
                {
                    NotificationId = notif.NotificationId,
                    UserId = uid,
                    IsRead = false,
                    DeliveredAt = nowUtc
                }).ToList();

                await _userNotificationRepo.CreateRangeAsync(userNotifs);

                // Mark & log
                await _notificationRepo.MarkSentAsync(notif.NotificationId, nowUtc);

                await _logRepo.AddAsync(new NotificationLog
                {
                    NotificationId = notif.NotificationId,
                    SentTo = GetSentToDescription(notif),
                    Result = "Success",
                    Details = $"Đã phát tới {recipients.Count} người nhận",
                    SentAt = nowUtc
                });

                return true;
            }
            catch (Exception ex)
            {
                await _logRepo.AddAsync(new NotificationLog
                {
                    NotificationId = notif.NotificationId,
                    SentTo = GetSentToDescription(notif),
                    Result = "Failed",
                    Details = ex.Message,
                    SentAt = nowUtc
                });
                return false;
            }
        }

        private async Task<List<int>> BuildRecipientsAsync(Notification notif)
        {
            var list = new List<int>();
            switch (NormalizeTargetType(notif.TargetType))
            {
                case "All":
                    {
                        var users = await _accountRepo.GetAllAsync();
                        list = users.Select(u => u.AccountId).ToList();
                        break;
                    }
                case "SingleUser":
                    {
                        if (notif.TargetUserId.HasValue)
                            list.Add(notif.TargetUserId.Value);
                        break;
                    }
                case "ByRole":
                    {
                        if (notif.TargetRoleId.HasValue)
                        {
                            var users = await _accountRepo.GetByRoleIdAsync(notif.TargetRoleId.Value);
                            list = users.Select(u => u.AccountId).ToList();
                        }
                        break;
                    }
                case "ByCondition":
                    {
                        // TODO: parse ConditionJson để lọc người dùng theo điều kiện (tuỳ business)
                        // Tạm thời: không ai
                        break;
                    }
            }
            return list.Distinct().ToList();
        }

        private string GetSentToDescription(Notification notif)
        {
            var tt = NormalizeTargetType(notif.TargetType);
            return tt switch
            {
                "All" => "Tất cả người dùng",
                "SingleUser" => notif.TargetUser?.Email ?? "User không xác định",
                "ByRole" => notif.TargetRole?.RoleName ?? $"RoleId={notif.TargetRoleId}",
                "ByCondition" => "Theo điều kiện",
                _ => "N/A"
            };
        }
    }
}
