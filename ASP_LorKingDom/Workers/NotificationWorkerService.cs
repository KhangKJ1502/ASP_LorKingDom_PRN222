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
    /// <summary>
    /// Worker gửi thông báo theo lịch. Chống chồng lặp, có jitter, và dùng PeriodicTimer.
    /// </summary>
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Interval cơ sở (có thể chuyển sang IOptions để cấu hình qua appsettings)
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        // Gate chống overlap
        private readonly SemaphoreSlim _gate = new(1, 1);

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService starting.");

            // Jitter nhẹ khi khởi động để tránh đồng bộ nhịp giữa nhiều instance
            var startupJitterMs = Random.Shared.Next(0, 5000);
            try
            {
                await Task.Delay(startupJitterMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // app đang dừng, thoát sớm
                return;
            }

            // Nếu muốn chạy 1 lần ngay khi start (bỏ comment):
            // await SafeProcessOnceAsync(stoppingToken);

            var timer = new PeriodicTimer(_interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // bình thường khi app shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationWorkerService crashed unexpectedly.");
            }
            finally
            {
                timer.Dispose();
                _logger.LogInformation("NotificationWorkerService stopping.");
            }
        }

        private async Task SafeProcessOnceAsync(CancellationToken ct)
        {
            // Tránh overlap: nếu gate đang bị giữ, bỏ qua nhịp này
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
                // tôn trọng hủy, không log error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing due notifications.");
            }
            finally
            {
                sw.Stop();
                _gate.Release();
                _logger.LogDebug("Dispatch run finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            }
        }

        private async Task ProcessDueNotificationsAsync(CancellationToken ct)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var count = await notificationService.DispatchDueAsync();

            if (count > 0)
            {
                _logger.LogInformation("Processed {Count} due notification(s) at {UtcTime}.",
                    count, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService is stopping gracefully.");
            return base.StopAsync(stoppingToken);
        }
    }
}
