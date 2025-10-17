
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

            services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
            services.AddScoped<IPriceRangeRepository, PriceRangeRepository>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();

            //NOTIFICATION
          services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
            services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
            // ✳️ Thêm dòng này:

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();


            return services;
        }
    }
}
