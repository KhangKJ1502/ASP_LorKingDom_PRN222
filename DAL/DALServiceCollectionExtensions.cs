using DAL.Data;
using DAL.Interfaces;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DAL
{
    public static class DALServiceCollectionExtensions
    {
        public static IServiceCollection AddDAL(this IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

            // Repo của bạn ...
            // services.AddScoped<IProductRepository, ProductRepository>();


            // ✳️ Thêm dòng này:
            services.AddScoped<IDbHealthCheck, DbHealthCheck>();

            return services;
        }
    }
}
