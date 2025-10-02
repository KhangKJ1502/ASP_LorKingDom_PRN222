using Microsoft.Extensions.DependencyInjection;
using BLL.Interfaces;
using BLL.Services;

namespace BLL
{
    public static class BLLServiceCollectionExtensions
    {
        public static IServiceCollection AddBLL(this IServiceCollection services)
        {
            // Services của bạn ...
            // services.AddScoped<IProductService, ProductService>();

            // ✳️ Thêm dòng này:
            services.AddScoped<ISystemHealthService, SystemHealthService>();

            return services;
        }
    }
}
