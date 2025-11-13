using BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WebUI.BackgroundServices
{


    public class NotificationWorkerService : BackgroundService
    {

        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Kiểm tra mỗi 1 phút
        private readonly SemaphoreSlim _gate = new(1, 1); // Đảm bảo chỉ 1 job chạy tại 1 thời điểm

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService STARTED. Interval: {Interval}", _interval);

            // Delay ngẫu nhiên 0-5s để tránh spike khi startup nhiều workers cùng lúc
            var startupJitterMs = Random.Shared.Next(0, 5000);
            try { await Task.Delay(startupJitterMs, stoppingToken); }
            catch (OperationCanceledException) { return; }

            var timer = new PeriodicTimer(_interval);
            try
            {
                // Loop chạy mãi cho đến khi application shutdown
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) 
            { 
                _logger.LogInformation("NotificationWorkerService cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationWorkerService crashed unexpectedly.");
            }
            finally
            {
                timer.Dispose();
                _logger.LogInformation("NotificationWorkerService STOPPED.");
            }
        }

        /// Wrapper đảm bảo:
        /// 1. Chỉ 1 job chạy tại 1 thời điểm (SemaphoreSlim)
        /// 2. Đo thời gian thực thi
        /// 3. Catch exceptions để worker không crash

        private async Task SafeProcessOnceAsync(CancellationToken ct)
        {
            // Kiểm tra xem job trước đã chạy xong chưa
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogWarning("Previous notification dispatch is still running; skipping this tick.");
                return;
            }

            var sw = Stopwatch.StartNew();
            try 
            { 
                await ProcessDueNotificationsAsync(ct); 
            }
            catch (OperationCanceledException) 
            { 
                _logger.LogDebug("Notification processing cancelled.");
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error occurred while processing due notifications."); 
            }
            finally
            {
                sw.Stop();
                _gate.Release();
                _logger.LogDebug("⏱️ Notification dispatch finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            }
        }


        /// Logic chính: Gửi notifications đến hạn
        /// - Tạo scoped service để có DbContext mới mỗi lần chạy
        /// - Gọi NotificationService.DispatchDueAsync()
        /// - Log số lượng notifications đã gửi

        private async Task ProcessDueNotificationsAsync(CancellationToken ct)
        {
            // Tạo scope mới để có DbContext mới (avoid tracking issues)
            await using var scope = _serviceProvider.CreateAsyncScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var count = await notificationService.DispatchDueAsync();
            
            if (count > 0)
            {
                _logger.LogInformation(
                    "Processed {Count} due notification(s) at {UtcTime}.",
                    count,
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                _logger.LogDebug("No due notifications at {UtcTime}.", 
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService is stopping gracefully...");
            return base.StopAsync(stoppingToken);
        }
    }
}

