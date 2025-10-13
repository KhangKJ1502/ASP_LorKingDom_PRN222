
using DAL.Interfaces;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using DAL.Models;
namespace DAL
{
    public static class DALServiceCollectionExtensions
    {
        public static IServiceCollection AddDAL(this IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<AspLorKingDomContext>(opt => opt.UseSqlServer(connectionString));

            // Repo của bạn ...
            // services.AddScoped<IProductRepository, ProductRepository>();

            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
            services.AddScoped<IPriceRangeRepository, PriceRangeRepository>();
            // ✳️ Thêm dòng này:


            return services;
        }
    }
}
