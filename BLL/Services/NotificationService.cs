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
    /// <summary>
    /// Service xử lý nghiệp vụ quản trị & worker cho thông báo.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserNotificationRepository _userNotificationRepo;
        private readonly INotificationLogRepository _logRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IRoleRepository _roleRepo;

        private static class LogResults
        {
            public const string Success = "Success";
            public const string Failed = "Failed";
            public const string Canceled = "Canceled";
            public const string Skipped = "Skipped";
        }

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
                Type = dto.Type,
                TargetType = dto.TargetType,
                TargetRoleId = dto.TargetRoleId,
                TargetUserId = dto.TargetUserId,
                ConditionJson = dto.ConditionJson?.Trim(),
                ScheduledAt = dto.ScheduledAt,
                ExpireAt = dto.ExpireAt,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                IsSent = false,
                IsCanceled = false
            };

            var created = await _notificationRepo.CreateAsync(entity);

            await _logRepo.AddAsync(new NotificationLog
            {
                NotificationId = created.NotificationId,
                Result = LogResults.Success,
                Details = $"Thông báo mới được tạo: {created.Title}",
                SentAt = DateTime.UtcNow
            });

            return MapToDto(created);
        }

        public async Task UpdateAsync(NotificationUpdateDto dto)
        {
            var entity = await _notificationRepo.GetByIdAsync(dto.NotificationId)
                         ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID {dto.NotificationId}");

            if (entity.IsSent) throw new InvalidOperationException("Không thể sửa thông báo đã gửi");
            if (entity.IsCanceled) throw new InvalidOperationException("Không thể sửa thông báo đã hủy");

            await ValidateUpdateAsync(dto, entity);

            entity.Title = dto.Title.Trim();
            entity.Message = dto.Message.Trim();
            entity.Type = dto.Type;
            entity.TargetType = dto.TargetType;
            entity.TargetRoleId = dto.TargetRoleId;
            entity.TargetUserId = dto.TargetUserId;
            entity.ConditionJson = dto.ConditionJson?.Trim();
            entity.ScheduledAt = dto.ScheduledAt;
            entity.ExpireAt = dto.ExpireAt;

            await _notificationRepo.UpdateAsync(entity);

            await _logRepo.AddAsync(new NotificationLog
            {
                NotificationId = dto.NotificationId,
                Result = LogResults.Success,
                Details = $"Thông báo được cập nhật: {dto.Title}",
                SentAt = DateTime.UtcNow
            });
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _notificationRepo.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Không tìm thấy thông báo ID {id}");

            if (entity.IsSent) throw new InvalidOperationException("Không thể xóa thông báo đã gửi");

            await _notificationRepo.DeleteAsync(id);

            await _logRepo.AddAsync(new NotificationLog
            {
                NotificationId = id,
                Result = LogResults.Success,
                Details = $"Thông báo bị xóa: {entity.Title}",
                SentAt = DateTime.UtcNow
            });
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
                Result = LogResults.Canceled,
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

        // =================== Worker ===================

        public async Task<int> DispatchDueAsync()
        {
            var now = DateTime.UtcNow;
            var due = await _notificationRepo.GetDuePendingAsync(now, 200);
            int success = 0;

            foreach (var notif in due)
            {
                try
                {
                    var ok = await DispatchOneAsync(notif, now);
                    if (ok) success++;
                }
                catch (Exception ex)
                {
                    await _logRepo.AddAsync(new NotificationLog
                    {
                        NotificationId = notif.NotificationId,
                        Result = LogResults.Failed,
                        Details = $"Lỗi khi xử lý: {ex.Message}",
                        SentAt = now
                    });
                }
            }
            return success;
        }

        // =================== Private helpers ===================

        private static NotificationDto MapToDto(Notification e) => new()
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
            TargetRoleName = e.TargetRole?.RoleName,
            TargetUserEmail = e.TargetUser?.Email,
            CreatedByEmail = e.CreatedByNavigation?.Email
        };

        private async Task ValidateCreateAsync(NotificationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Tiêu đề không được rỗng");
            if (dto.Title.Length > 200)
                throw new InvalidOperationException("Tiêu đề không được vượt quá 200 ký tự");
            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new InvalidOperationException("Nội dung không được rỗng");
            if (dto.Message.Length > 1000)
                throw new InvalidOperationException("Nội dung không được vượt quá 1000 ký tự");

            if (await _notificationRepo.ExistsByTitleAsync(dto.Title.Trim()))
                throw new InvalidOperationException($"Tiêu đề '{dto.Title}' đã tồn tại");

            if (!IsValidType(dto.Type))
                throw new InvalidOperationException($"Type '{dto.Type}' không hợp lệ. Phải là: General, Order, Promotion, System");
            if (!IsValidTargetType(dto.TargetType))
                throw new InvalidOperationException($"TargetType '{dto.TargetType}' không hợp lệ. Phải là: All, SingleUser, ByRole, ByCondition");

            await ValidateTargetType(dto.TargetType, dto.TargetRoleId, dto.TargetUserId, dto.ConditionJson);

            var minAllowedTime = DateTime.UtcNow.AddMinutes(-1);
            if (dto.ScheduledAt < minAllowedTime)
                throw new InvalidOperationException("Thời gian gửi không được ở quá khứ");

            if (dto.ExpireAt.HasValue && dto.ExpireAt.Value <= dto.ScheduledAt)
                throw new InvalidOperationException("Thời gian hết hạn phải sau thời gian hiển thị");
        }

        private async Task ValidateUpdateAsync(NotificationUpdateDto dto, Notification existingEntity)
        {
            if (dto.Title.Length > 200)
                throw new InvalidOperationException("Tiêu đề không được vượt quá 200 ký tự");
            if (dto.Message.Length > 1000)
                throw new InvalidOperationException("Nội dung không được vượt quá 1000 ký tự");

            if (!string.Equals(existingEntity.Title, dto.Title.Trim(), StringComparison.Ordinal))
            {
                var duplicated = await _notificationRepo.ExistsByTitleAsync(dto.Title.Trim(), dto.NotificationId);
                if (duplicated)
                    throw new InvalidOperationException($"Tiêu đề '{dto.Title}' đã tồn tại");
            }

            if (!IsValidType(dto.Type))
                throw new InvalidOperationException($"Type '{dto.Type}' không hợp lệ");
            if (!IsValidTargetType(dto.TargetType))
                throw new InvalidOperationException($"TargetType '{dto.TargetType}' không hợp lệ");

            await ValidateTargetType(dto.TargetType, dto.TargetRoleId, dto.TargetUserId, dto.ConditionJson);

            var minAllowedTime = DateTime.UtcNow.AddMinutes(-1);
            if (dto.ScheduledAt < minAllowedTime)
                throw new InvalidOperationException("Thời gian gửi không được ở quá khứ");

            if (dto.ExpireAt.HasValue && dto.ExpireAt.Value <= dto.ScheduledAt)
                throw new InvalidOperationException("Thời gian hết hạn phải sau thời gian hiển thị");
        }

        private async Task ValidateTargetType(string targetType, int? targetRoleId, int? targetUserId, string? conditionJson)
        {
            switch (targetType)
            {
                case "SingleUser":
                    if (!targetUserId.HasValue || targetUserId.Value <= 0)
                        throw new InvalidOperationException("Phải chỉ định TargetUserId hợp lệ khi TargetType = SingleUser");
                    var user = await _accountRepo.GetByIdAsync(targetUserId.Value)
                               ?? throw new InvalidOperationException($"User ID {targetUserId.Value} không tồn tại");
                    break;

                case "ByRole":
                    if (!targetRoleId.HasValue || targetRoleId.Value <= 0)
                        throw new InvalidOperationException("Phải chỉ định TargetRoleId hợp lệ khi TargetType = ByRole");
                    var role = await _roleRepo.GetByIdAsync(targetRoleId.Value)
                               ?? throw new InvalidOperationException($"Role ID {targetRoleId.Value} không tồn tại");
                    break;

                case "ByCondition":
                    // TODO: validate conditionJson khi implement filter
                    _ = conditionJson;
                    break;

                case "All":
                    break;
            }
        }

        private static bool IsValidType(string type) =>
            type is "General" or "Order" or "Promotion" or "System";

        private static bool IsValidTargetType(string targetType) =>
            targetType is "All" or "SingleUser" or "ByRole" or "ByCondition";

        private async Task<bool> DispatchOneAsync(Notification notif, DateTime nowUtc)
        {
            try
            {
                if (notif.ExpireAt.HasValue && notif.ExpireAt.Value <= nowUtc)
                {
                    await _logRepo.AddAsync(new NotificationLog
                    {
                        NotificationId = notif.NotificationId,
                        SentTo = GetSentToDescription(notif),
                        Result = LogResults.Skipped,
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
                        Result = LogResults.Skipped,
                        Details = "Không có người nhận",
                        SentAt = nowUtc
                    });
                    await _notificationRepo.MarkSentAsync(notif.NotificationId, nowUtc);
                    return true;
                }

                var userNotifs = recipients.Select(uid => new UserNotification
                {
                    NotificationId = notif.NotificationId,
                    UserId = uid,
                    IsRead = false,
                    DeliveredAt = nowUtc
                }).ToList();

                await _userNotificationRepo.CreateRangeAsync(userNotifs);

                await _notificationRepo.MarkSentAsync(notif.NotificationId, nowUtc);

                await _logRepo.AddAsync(new NotificationLog
                {
                    NotificationId = notif.NotificationId,
                    SentTo = GetSentToDescription(notif),
                    Result = LogResults.Success,
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
                    Result = LogResults.Failed,
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
                        // TODO: parse ConditionJson & filter nâng cao
                        break;
                    }
            }

            return list.Distinct().ToList();
        }

        private static string GetSentToDescription(Notification notif) =>
            notif.TargetType switch
            {
                "All" => "Tất cả người dùng",
                "SingleUser" => notif.TargetUser?.Email ?? $"UserID={notif.TargetUserId}",
                "ByRole" => notif.TargetRole?.RoleName ?? $"RoleID={notif.TargetRoleId}",
                "ByCondition" => "Theo điều kiện",
                _ => "N/A"
            };
    }
}
