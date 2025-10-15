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
            //services.AddScoped<ISystemHealthService, SystemHealthService>();
            services.AddScoped<IOriginService, OriginService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMaterialService, MaterialService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<ISuperCategoryService, SuperCategoryService>();
            services.AddScoped<IPriceRangeService, PriceRangeService>();
            return services;
        }
    }
}
