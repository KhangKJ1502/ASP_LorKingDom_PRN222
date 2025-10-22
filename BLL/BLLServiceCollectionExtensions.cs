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

            // Nhánh Authentication
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IEmailOtpService, EmailOtpService>();

            // Nhánh Role
            services.AddScoped<IRoleService, RoleService>();

            // Nhánh Blog
            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IBlogCategoryService, BlogCategoryService>();

            //Nhánh Product
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductImageService, ProductImageService>();
            services.AddScoped<ISexService, SexService>();
            services.AddScoped<IAgeService, AgeService>();
            services.AddScoped<IOriginService, OriginService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMaterialService, MaterialService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<ISuperCategoryService, SuperCategoryService>();
            services.AddScoped<IPriceRangeService, PriceRangeService>();
            services.AddScoped<IPromotionService, PromotionService>();
            services.AddScoped<PromotionValidator>();

            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatService, ChatService>();

            // Nhánh ReviewBlog
            services.AddScoped<IReviewBlogService, ReviewBlogService>();
            services.AddScoped<IReviewBlogReactionService, ReviewBlogReactionService>();
            services.AddScoped<IReviewBlogReplyService, ReviewBlogReplyService>();

            return services;
        }
    }
}
