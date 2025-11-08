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

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
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

            var startupJitterMs = Random.Shared.Next(0, 5000);
            try { await Task.Delay(startupJitterMs, stoppingToken); }
            catch (OperationCanceledException) { return; }

            var timer = new PeriodicTimer(_interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { }
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
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogWarning("Previous notification dispatch is still running; skipping this tick.");
                return;
            }

            var sw = Stopwatch.StartNew();
            try { await ProcessDueNotificationsAsync(ct); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogError(ex, "Error occurred while processing due notifications."); }
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
                _logger.LogInformation(
                    "Processed {Count} due notification(s) at {UtcTime}.",
                    count,
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService is stopping gracefully.");
            return base.StopAsync(stoppingToken);
        }
    }
}
