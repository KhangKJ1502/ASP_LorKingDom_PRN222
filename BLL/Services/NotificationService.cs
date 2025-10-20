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
                Type = dto.Type, // Đã normalize từ Controller
                TargetType = dto.TargetType, // Đã normalize từ Controller
                TargetRoleId = dto.TargetRoleId,
                TargetUserId = dto.TargetUserId,
                ConditionJson = dto.ConditionJson,
                ScheduledAt = dto.ScheduledAt, // Đã convert UTC từ Controller
                ExpireAt = dto.ExpireAt, // Đã convert UTC từ Controller
                CreatedBy = 10,
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

            entity.Title = dto.Title.Trim();
            entity.Message = dto.Message.Trim();
            entity.Type = dto.Type; // Đã normalize từ Controller
            entity.ScheduledAt = dto.ScheduledAt; // Đã convert UTC từ Controller
            entity.ExpireAt = dto.ExpireAt; // Đã convert UTC từ Controller

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
                Type = x.Notification?.Type ?? "General",
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
            await _userNotificationRepo.MarkReadAsync(userNotificationId, DateTime.UtcNow);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var (_, total) = await _userNotificationRepo.GetByUserAsync(userId, isRead: false, page: 1, pageSize: 1);
            return total;
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
                CreatedAt = e.CreatedAt,
                // Navigation properties
                TargetRoleName = e.TargetRole?.RoleName,
                TargetUserEmail = e.TargetUser?.Email,
                CreatedByEmail = e.CreatedByNavigation?.Email
            };
        }

        private async Task ValidateCreateAsync(NotificationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Title không được rỗng");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new InvalidOperationException("Message không được rỗng");

            if (await _notificationRepo.ExistsByTitleAsync(dto.Title.Trim()))
                throw new InvalidOperationException($"Tiêu đề '{dto.Title}' đã tồn tại");

            // Validate Type (đã normalize từ Controller)
            if (!IsValidType(dto.Type))
                throw new InvalidOperationException($"Type '{dto.Type}' không hợp lệ. Phải là: General, Order, Promotion, System");

            // Validate TargetType (đã normalize từ Controller)
            if (!IsValidTargetType(dto.TargetType))
                throw new InvalidOperationException($"TargetType '{dto.TargetType}' không hợp lệ. Phải là: All, SingleUser, ByRole, ByCondition");

            switch (dto.TargetType)
            {
                case "SingleUser":
                    if (!dto.TargetUserId.HasValue)
                        throw new InvalidOperationException("Phải chỉ định TargetUserId khi TargetType = SingleUser");

                    var user = await _accountRepo.GetByIdAsync(dto.TargetUserId.Value);
                    if (user == null)
                        throw new InvalidOperationException($"User ID {dto.TargetUserId.Value} không tồn tại");
                    break;

                case "ByRole":
                    if (!dto.TargetRoleId.HasValue)
                        throw new InvalidOperationException("Phải chỉ định TargetRoleId khi TargetType = ByRole");

                    var role = await _roleRepo.GetByIdAsync(dto.TargetRoleId.Value);
                    if (role == null)
                        throw new InvalidOperationException($"Role ID {dto.TargetRoleId.Value} không tồn tại");
                    break;

                case "ByCondition":
                    if (string.IsNullOrWhiteSpace(dto.ConditionJson))
                        throw new InvalidOperationException("Phải chỉ định ConditionJson khi TargetType = ByCondition");
                    break;

                case "All":
                    // Không cần validate gì thêm
                    break;
            }

            var now = DateTime.UtcNow.AddMinutes(-5);
            if (dto.ScheduledAt < now)
                throw new InvalidOperationException("Thời gian gửi không được ở quá khứ");

            if (dto.ExpireAt.HasValue && dto.ExpireAt.Value <= dto.ScheduledAt)
                throw new InvalidOperationException("ExpireAt phải sau ScheduledAt");
        }

        private static bool IsValidType(string type)
        {
            return type is "General" or "Order" or "Promotion" or "System";
        }

        private static bool IsValidTargetType(string targetType)
        {
            return targetType is "All" or "SingleUser" or "ByRole" or "ByCondition";
        }

        private async Task<bool> DispatchOneAsync(Notification notif, DateTime nowUtc)
        {
            try
            {
                // Bỏ qua nếu đã hết hạn
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
                    await _notificationRepo.MarkSentAsync(notif.NotificationId, nowUtc);
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
                    await _notificationRepo.MarkSentAsync(notif.NotificationId, nowUtc);
                    return true;
                }

                // Tạo bản ghi UserNotifications
                var userNotifs = recipients.Select(uid => new UserNotification
                {
                    NotificationId = notif.NotificationId,
                    UserId = uid,
                    IsRead = false,
                    DeliveredAt = nowUtc
                }).ToList();

                await _userNotificationRepo.CreateRangeAsync(userNotifs);

                // Mark sent & log
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
            switch (notif.TargetType)
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
                        // TODO: parse ConditionJson để lọc người dùng theo điều kiện
                        break;
                    }
            }
            return list.Distinct().ToList();
        }

        private string GetSentToDescription(Notification notif)
        {
            return notif.TargetType switch
            {
                "All" => "Tất cả người dùng",
                "SingleUser" => notif.TargetUser?.Email ?? $"UserID={notif.TargetUserId}",
                "ByRole" => notif.TargetRole?.RoleName ?? $"RoleID={notif.TargetRoleId}",
                "ByCondition" => "Theo điều kiện",
                _ => "N/A"
            };
        }
    }
}