// WebUI/Workers/PromotionWorkerService.cs
using BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WebUI.Workers
{
    /// <summary>
    /// Background service tự động kiểm tra và xử lý:
    /// - Promotions hết hạn → set Status = "Inactive"
    /// - Promotions sắp bắt đầu → có thể gửi thông báo (tùy chọn)
    /// </summary>
    public class PromotionWorkerService : BackgroundService
    {
        private readonly ILogger<PromotionWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Kiểm tra mỗi 5 phút (có thể điều chỉnh)
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly SemaphoreSlim _gate = new(1, 1);

        public PromotionWorkerService(
            ILogger<PromotionWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PromotionWorkerService starting. Check interval: {Interval}", _interval);

            // Delay ngẫu nhiên để tránh spike khi startup
            var startupJitterMs = Random.Shared.Next(0, 3000);
            try { await Task.Delay(startupJitterMs, stoppingToken); }
            catch (OperationCanceledException) { return; }

            var timer = new PeriodicTimer(_interval);
            try
            {
                // Chạy ngay lần đầu
                await SafeProcessOnceAsync(stoppingToken);

                // Sau đó chạy theo interval
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PromotionWorkerService cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PromotionWorkerService crashed unexpectedly.");
            }
            finally
            {
                timer.Dispose();
                _logger.LogInformation("PromotionWorkerService stopping.");
            }
        }

        private async Task SafeProcessOnceAsync(CancellationToken ct)
        {
            // Kiểm tra xem có job khác đang chạy không
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogWarning("Previous promotion check is still running; skipping this tick.");
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                await ProcessExpiredPromotionsAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired promotions.");
            }
            finally
            {
                sw.Stop();
                _gate.Release();
                _logger.LogDebug("Promotion check finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            }
        }

        private async Task ProcessExpiredPromotionsAsync(CancellationToken ct)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<IPromotionService>();

            try
            {
                // Lấy tất cả promotions đang Active
                var activePromotions = await promotionService.GetActiveAsync();
                var now = DateTime.Now;
                int expiredCount = 0;
                int notStartedCount = 0;

                foreach (var promo in activePromotions)
                {
                    // Kiểm tra nếu đã hết hạn
                    if (now > promo.EndDate)
                    {
                        _logger.LogInformation(
                            "Promotion '{Code}' (ID: {Id}) has expired. EndDate: {EndDate}, Current: {Now}",
                            promo.PromotionCode, promo.PromotionId, promo.EndDate, now);

                        // Tự động set về Inactive
                        try
                        {
                            var updateDto = new BLL.DTOs.PromotionUpdateDto
                            {
                                PromotionId = promo.PromotionId,
                                PromotionCode = promo.PromotionCode,
                                Description = promo.Description,
                                DiscountPercent = promo.DiscountPercent,
                                StartDate = promo.StartDate,
                                EndDate = promo.EndDate,
                                Status = "Inactive" // Set về Inactive
                            };

                            await promotionService.UpdateAsync(updateDto);
                            expiredCount++;

                            _logger.LogInformation(
                                "Successfully set promotion '{Code}' to Inactive.",
                                promo.PromotionCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Failed to set promotion '{Code}' to Inactive.",
                                promo.PromotionCode);
                        }
                    }
                    // Kiểm tra nếu chưa bắt đầu (optional warning)
                    else if (now < promo.StartDate)
                    {
                        notStartedCount++;
                        _logger.LogDebug(
                            "Promotion '{Code}' (ID: {Id}) is Active but hasn't started yet. StartDate: {StartDate}",
                            promo.PromotionCode, promo.PromotionId, promo.StartDate);
                    }
                }

                if (expiredCount > 0 || notStartedCount > 0)
                {
                    _logger.LogInformation(
                        "Promotion check completed: {Expired} expired, {NotStarted} not started yet.",
                        expiredCount, notStartedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking promotions.");
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PromotionWorkerService is stopping gracefully.");
            return base.StopAsync(stoppingToken);
        }
    }
}
