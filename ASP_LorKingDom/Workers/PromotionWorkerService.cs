using BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebUI.Workers
{

    public class PromotionWorkerService : BackgroundService
    {

        private readonly ILogger<PromotionWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

      
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Kiểm tra mỗi 5 phút
        private readonly SemaphoreSlim _gate = new(1, 1); // Đảm bảo chỉ 1 job chạy tại 1 thời điểm

        public PromotionWorkerService(
            ILogger<PromotionWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("========== [START] PromotionWorkerService STARTED at {StartTime:yyyy-MM-dd HH:mm:ss.fff}. Interval: {Interval} ==========", DateTime.Now, _interval);

            // Delay ngẫu nhiên 0-3s để tránh spike khi startup nhiều workers
            var startupJitterMs = Random.Shared.Next(0, 3000);
            try { await Task.Delay(startupJitterMs, stoppingToken); }
            catch (OperationCanceledException) { return; }

            var timer = new PeriodicTimer(_interval);
            try
            {
                // Chạy ngay lần đầu tiên (không đợi interval đầu tiên)
                _logger.LogInformation("[RUN #1] Running initial promotion check at {Time:HH:mm:ss}...", DateTime.Now);
                await SafeProcessOnceAsync(stoppingToken);

                // Sau đó chạy theo interval
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    _logger.LogInformation("[RUN] Periodic promotion check triggered at {Time:HH:mm:ss}...", DateTime.Now);
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
                _logger.LogInformation("========== [STOP] PromotionWorkerService STOPPED at {StopTime:HH:mm:ss} ==========", DateTime.Now);
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
                _logger.LogWarning("Previous promotion check is still running; skipping this tick.");
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                await ProcessExpiredPromotionsAsync(ct);
            }
            catch (OperationCanceledException) 
            { 
                _logger.LogDebug("Promotion processing cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired promotions.");
            }
            finally
            {
                sw.Stop();
                _gate.Release();
                _logger.LogInformation("[DONE] Promotion check completed in {ElapsedMs}ms at {EndTime:HH:mm:ss}", sw.ElapsedMilliseconds, DateTime.Now);
            }
        }

        private async Task ProcessExpiredPromotionsAsync(CancellationToken ct)
        {
            // Tạo scope mới để có DbContext mới
            await using var scope = _serviceProvider.CreateAsyncScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<IPromotionService>();

            try
            {
                var activePromotions = await promotionService.GetActiveAsync();
                var now = DateTime.Now; // Use Local time to match database DateTime (not UTC)
                int expiredCount = 0;
                int notStartedCount = 0;

                _logger.LogInformation("[SCAN] at {Now:HH:mm:ss} - Found {Count} active promotions", now, activePromotions.Count);

                // ===== BƯỚC 2: Xử lý từng promotion =====
                foreach (var promo in activePromotions)
                {
                    _logger.LogDebug("[DEBUG] Checking promotion: Code={Code}, ID={Id}, EndDate={End}, Now={Now}",
                        promo.PromotionCode, promo.PromotionId, 
                        promo.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        now.ToString("yyyy-MM-dd HH:mm:ss"));

                    // Kiểm tra nếu EndDate đã quá hạn
                    if (now >= promo.EndDate)
                    {
                        _logger.LogInformation(
                            "[EXPIRED] Promotion Code={Code}, ID={Id} | EndDate={EndDate} <= Now={Now}",
                            promo.PromotionCode, promo.PromotionId, promo.EndDate.ToString("yyyy-MM-dd HH:mm"), now.ToString("yyyy-MM-dd HH:mm"));

                        // Tự động set về Expired (theo DB schema)
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
                                Status = "Expired"
                            };

                            _logger.LogInformation("[DEBUG] Attempting to update promotion '{Code}' to Expired...", promo.PromotionCode);
                            var updateResult = await promotionService.UpdateAsync(updateDto);
                            
                            if (updateResult)
                            {
                                expiredCount++;
                                _logger.LogInformation(
                                    "[SUCCESS] Promotion '{Code}' (ID: {Id}) updated to Expired. Total expired so far: {Count}",
                                    promo.PromotionCode, promo.PromotionId, expiredCount);
                            }
                            else
                            {
                                _logger.LogError(
                                    "[ERROR] Update returned FALSE for promotion '{Code}' (ID: {Id}). Check validation or database.",
                                    promo.PromotionCode, promo.PromotionId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "[EXCEPTION] While updating promotion '{Code}' (ID: {Id}) to Expired.",
                                promo.PromotionCode, promo.PromotionId);
                        }
                    }
                }

                if (expiredCount > 0)
                {
                    _logger.LogInformation(
                        "[RESULT] Check at {Now:HH:mm:ss} -> {Expired} EXPIRED (set Expired)",
                        DateTime.Now, expiredCount);
                }
                else
                {
                    _logger.LogInformation("[RESULT] No expired promotions found at {Now:HH:mm:ss}. NO ACTION.", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking promotions.");
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PromotionWorkerService is stopping gracefully...");
            return base.StopAsync(stoppingToken);
        }
    }
}
