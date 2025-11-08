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
    /// ==================== PROMOTION WORKER SERVICE ====================
    /// Background service tự động quản lý lifecycle của Promotions:
    /// 
    /// CHỨC NĂNG:
    /// 1. Kiểm tra promotions đã HẾT HẠN → Set Status = "Inactive"
    /// 2. Kiểm tra promotions CHƯA BẮT ĐẦU → Warning log (optional)
    /// 3. Chạy mỗi 5 phút (có thể cấu hình)
    /// 
    /// CÁCH THAY ĐỔI INTERVAL:
    /// - Sửa _interval = TimeSpan.FromMinutes(X)
    /// - X khuyến nghị: 5-60 phút (không cần quá thường xuyên)
    /// 
    /// CÁCH TẮT WORKER:
    /// - Comment dòng trong Program.cs:
    ///   // builder.Services.AddHostedService<PromotionWorkerService>();
    /// =====================================================================
    /// </summary>
    public class PromotionWorkerService : BackgroundService
    {
        // ==================== DEPENDENCIES ====================
        private readonly ILogger<PromotionWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // ==================== CONFIGURATION ====================
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Kiểm tra mỗi 5 phút
        private readonly SemaphoreSlim _gate = new(1, 1); // Đảm bảo chỉ 1 job chạy tại 1 thời điểm

        public PromotionWorkerService(
            ILogger<PromotionWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        // ==================== MAIN EXECUTION ====================
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ PromotionWorkerService STARTED. Interval: {Interval}", _interval);

            // Delay ngẫu nhiên 0-3s để tránh spike khi startup nhiều workers
            var startupJitterMs = Random.Shared.Next(0, 3000);
            try { await Task.Delay(startupJitterMs, stoppingToken); }
            catch (OperationCanceledException) { return; }

            var timer = new PeriodicTimer(_interval);
            try
            {
                // Chạy ngay lần đầu tiên (không đợi interval đầu tiên)
                _logger.LogInformation("🔄 Running initial promotion check...");
                await SafeProcessOnceAsync(stoppingToken);

                // Sau đó chạy theo interval
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("⏸️ PromotionWorkerService cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 PromotionWorkerService crashed unexpectedly.");
            }
            finally
            {
                timer.Dispose();
                _logger.LogInformation("⛔ PromotionWorkerService STOPPED.");
            }
        }

        // ==================== SAFE EXECUTION WRAPPER ====================
        /// <summary>
        /// Wrapper đảm bảo:
        /// 1. Chỉ 1 job chạy tại 1 thời điểm (SemaphoreSlim)
        /// 2. Đo thời gian thực thi
        /// 3. Catch exceptions để worker không crash
        /// </summary>
        private async Task SafeProcessOnceAsync(CancellationToken ct)
        {
            // Kiểm tra xem job trước đã chạy xong chưa
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogWarning("⚠️ Previous promotion check is still running; skipping this tick.");
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                await ProcessExpiredPromotionsAsync(ct);
            }
            catch (OperationCanceledException) 
            { 
                _logger.LogDebug("🛑 Promotion processing cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while processing expired promotions.");
            }
            finally
            {
                sw.Stop();
                _gate.Release();
                _logger.LogDebug("⏱️ Promotion check finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            }
        }

        // ==================== BUSINESS LOGIC ====================
        /// <summary>
        /// Logic chính: Kiểm tra và cập nhật promotions
        /// 
        /// FLOW:
        /// 1. Lấy tất cả promotions đang Active
        /// 2. Với mỗi promotion:
        ///    - Nếu HẾT HẠN (now > EndDate) → Set Status = "Inactive"
        ///    - Nếu CHƯA BẮT ĐẦU (now < StartDate) → Log warning
        /// 3. Log tổng kết số lượng đã xử lý
        /// </summary>
        private async Task ProcessExpiredPromotionsAsync(CancellationToken ct)
        {
            // Tạo scope mới để có DbContext mới
            await using var scope = _serviceProvider.CreateAsyncScope();
            var promotionService = scope.ServiceProvider.GetRequiredService<IPromotionService>();

            try
            {
                // ===== BƯỚC 1: Lấy tất cả promotions Active =====
                var activePromotions = await promotionService.GetActiveAsync();
                var now = DateTime.Now;
                int expiredCount = 0;
                int notStartedCount = 0;

                _logger.LogDebug("🔍 Checking {Count} active promotions...", activePromotions.Count());

                // ===== BƯỚC 2: Xử lý từng promotion =====
                foreach (var promo in activePromotions)
                {
                    // *** TRƯỜNG HỢP 1: Promotion đã HẾT HẠN ***
                    if (now > promo.EndDate)
                    {
                        _logger.LogInformation(
                            "⏰ Promotion '{Code}' (ID: {Id}) EXPIRED. EndDate: {EndDate}, Now: {Now}",
                            promo.PromotionCode, promo.PromotionId, promo.EndDate.ToString("yyyy-MM-dd HH:mm"), now.ToString("yyyy-MM-dd HH:mm"));

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
                                "✅ Successfully set promotion '{Code}' to Inactive.",
                                promo.PromotionCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "❌ Failed to set promotion '{Code}' to Inactive.",
                                promo.PromotionCode);
                        }
                    }
                    // *** TRƯỜNG HỢP 2: Promotion CHƯA BẮT ĐẦU (optional warning) ***
                    else if (now < promo.StartDate)
                    {
                        notStartedCount++;
                        _logger.LogDebug(
                            "📅 Promotion '{Code}' (ID: {Id}) is Active but NOT STARTED yet. StartDate: {StartDate}",
                            promo.PromotionCode, promo.PromotionId, promo.StartDate.ToString("yyyy-MM-dd HH:mm"));
                    }
                    // TRƯỜNG HỢP 3: Promotion đang trong thời gian active → OK
                }

                // ===== BƯỚC 3: Log tổng kết =====
                if (expiredCount > 0 || notStartedCount > 0)
                {
                    _logger.LogInformation(
                        "📊 Promotion check completed: {Expired} expired → Inactive, {NotStarted} not started yet.",
                        expiredCount, notStartedCount);
                }
                else
                {
                    _logger.LogDebug("✔️ All promotions are in valid time range.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error while checking promotions.");
            }
        }

        // ==================== GRACEFUL SHUTDOWN ====================
        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 PromotionWorkerService is stopping gracefully...");
            return base.StopAsync(stoppingToken);
        }
    }
}

// ==================== USAGE NOTES ====================
// 1. Worker này được đăng ký trong Program.cs:
//    builder.Services.AddHostedService<PromotionWorkerService>();
//
// 2. Worker sẽ:
//    - Chạy NGAY khi application start (initial check)
//    - Sau đó chạy theo interval (mặc định 5 phút)
//
// 3. Để thay đổi interval:
//    - Sửa: private readonly TimeSpan _interval = TimeSpan.FromMinutes(X);
//    - Khuyến nghị: 5-60 phút (không cần quá thường xuyên)
//
// 4. Để tạm tắt worker:
//    - Comment dòng AddHostedService trong Program.cs
//
// 5. Monitor worker:
//    - Xem logs: Tìm "PromotionWorkerService" trong console
//    - Check database: Xem bảng Promotions → Status
//
// 6. Logic tự động:
//    ✅ Promotion hết hạn → Status = "Inactive"
//    ⚠️ Promotion chưa bắt đầu → Warning log (không thay đổi)
//    ✔️ Promotion đang active → Không làm gì
// =====================================================================
