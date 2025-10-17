using BLL.Interfaces;
using BLL.Services;
using BLL.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace BLL
{
    public static class BLLServiceCollectionExtensions
    {
        public static IServiceCollection AddBLL(this IServiceCollection services)
        {
            // Services của bạn ...
            // services.AddScoped<IProductService, ProductService>();

            // ✳️ Thêm dòng này:
            //services.AddScoped<ISystemHealthService, SystemHealthService>();

            services.AddScoped<ISuperCategoryService, SuperCategoryService>();
            services.AddScoped<IPriceRangeService, PriceRangeService>();
            services.AddScoped<IPromotionService, PromotionService>();
            services.AddScoped<PromotionValidator>();

            services.AddScoped<INotificationService, NotificationService>();
            return services;
        }
    }
}
