
using DAL.Interfaces;
using DAL.Models;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace DAL
{
    public static class DALServiceCollectionExtensions
    {
        public static IServiceCollection AddDAL(this IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<AspLorKingDomContext>(opt => opt.UseSqlServer(connectionString));

            // services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();

            // Nhánh Authentication
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();

            // Nhánh Blog
            services.AddScoped<IBlogRepository, BlogRepository>();
            services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();

            //Nhánh Product 
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IAgeRepository, AgeRepository>();
            services.AddScoped<IOriginRepository, OriginRepository>();
            services.AddScoped<ISexRepository, SexRepository>();
            services.AddScoped<IMaterialRepository, MaterialRepository>();
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<ISuperCategoryRepository, SuperCategoryRepository>();
            services.AddScoped<IPriceRangeRepository, PriceRangeRepository>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();

            //WishList
            services.AddScoped<IWishlistRepository, WishlistRepository>();


            //NOTIFICATION
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
            services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

            //ReviewBlog
            services.AddScoped<IReviewBlogRepository, ReviewBlogRepository>();
            services.AddScoped<IReviewBlogReactionRepository, ReviewBlogReactionRepository>();
            services.AddScoped<IReviewBlogReplyRepository, ReviewBlogReplyRepository>();

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddSingleton<IChatStoreRepository, InMemoryChatStoreRepository>();

            //Review Product
            services.AddScoped<IReviewProductRepository, ReviewProductRepository>();
            services.AddScoped<IReviewProductReplyRepository, ReviewProductReplyRepository>();
            services.AddScoped<IReviewProductReactionRepository, ReviewProductReactionRepository>();
            services.AddScoped<IReviewProductImageRepository, ReviewProductImageRepository>();

            // Nhánh Voucher
            services.AddScoped<IVoucherRepository, VoucherRepository>();
            services.AddScoped<IVoucherTypeRepository, VoucherTypeRepository>();

            // Nhánh Cart
            services.AddScoped<ICartRepository, CartRepository>();

            // Nhánh Order
            services.AddScoped<IOrderRepository, OrderRepository>();

            //Order 
            services.AddScoped<IOrderRefundRepository, OrderRefundRepository>();

            //Statistics
            services.AddScoped<IStatisticsRepository, StatisticsRepository>();

            // Nhánh Wallets
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();

            return services;
        }
    }
}
