using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DAL.Models;

public partial class AspLorKingDomContext : DbContext
{
    public AspLorKingDomContext()
    {
    }

    public AspLorKingDomContext(DbContextOptions<AspLorKingDomContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Age> Ages { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailOtp> EmailOtps { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationLog> NotificationLogs { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderRefund> OrderRefunds { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Origin> Origins { get; set; }

    public virtual DbSet<PaymentHistory> PaymentHistories { get; set; }

    public virtual DbSet<PriceRange> PriceRanges { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<ReviewBlog> ReviewBlogs { get; set; }

    public virtual DbSet<ReviewBlogReaction> ReviewBlogReactions { get; set; }

    public virtual DbSet<ReviewBlogReply> ReviewBlogReplies { get; set; }

    public virtual DbSet<ReviewProduct> ReviewProducts { get; set; }

    public virtual DbSet<ReviewProductImage> ReviewProductImages { get; set; }

    public virtual DbSet<ReviewProductReaction> ReviewProductReactions { get; set; }

    public virtual DbSet<ReviewProductReply> ReviewProductReplies { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sex> Sexes { get; set; }

    public virtual DbSet<StatusOrder> StatusOrders { get; set; }

    public virtual DbSet<SuperCategory> SuperCategories { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherType> VoucherTypes { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-T27O90D\\SQLEXPRESS;Database=ASP_LorKingDom;User ID=sa;Password=khangmc1502@;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__349DA586FDF63712");

            entity.HasIndex(e => e.PhoneNumber, "UQ__Accounts__85FB4E38BE0ADDB0").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Accounts__A9D10534CF607743").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Image).HasMaxLength(500);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Provider).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Accounts_Roles");
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__091C2A1B4017DD02");

            entity.Property(e => e.AddressId).HasColumnName("AddressID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AddressLine).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Ward).HasMaxLength(100);

            entity.HasOne(d => d.Account).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Addresses_Accounts");
        });

        modelBuilder.Entity<Age>(entity =>
        {
            entity.HasKey(e => e.AgeId).HasName("PK__Ages__875454C2E4097EEA");

            entity.HasIndex(e => e.AgeRange, "UQ__Ages__E0EBEE381ECD80D4").IsUnique();

            entity.Property(e => e.AgeId).HasColumnName("AgeID");
            entity.Property(e => e.AgeRange).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.BlogCategoryId).HasName("PK__BlogCate__6BD2DA617D10FC3E");

            entity.HasIndex(e => e.BlogCategoryName, "UQ__BlogCate__06725EA7DF914E89").IsUnique();

            entity.Property(e => e.BlogCategoryId).HasColumnName("BlogCategoryID");
            entity.Property(e => e.BlogCategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.BlogPostId).HasName("PK__BlogPost__321741499BFA5BDF");

            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlogThumbnail).HasMaxLength(500);
            entity.Property(e => e.BlogTitle).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPosts_Accounts");

            entity.HasMany(d => d.BlogCategories).WithMany(p => p.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    r => r.HasOne<BlogCategory>().WithMany()
                        .HasForeignKey("BlogCategoryId")
                        .HasConstraintName("FK_BlogPostCategories_Categories"),
                    l => l.HasOne<BlogPost>().WithMany()
                        .HasForeignKey("BlogPostId")
                        .HasConstraintName("FK_BlogPostCategories_Posts"),
                    j =>
                    {
                        j.HasKey("BlogPostId", "BlogCategoryId").HasName("PK__BlogPost__34AA6CEFA55A7173");
                        j.ToTable("BlogPostCategories");
                        j.IndexerProperty<int>("BlogPostId").HasColumnName("BlogPostID");
                        j.IndexerProperty<int>("BlogCategoryId").HasColumnName("BlogCategoryID");
                    });
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__Brands__DAD4F3BE61726EDD");

            entity.HasIndex(e => e.BrandName, "UQ__Brands__2206CE9BB53C4878").IsUnique();

            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD79787931B5C");

            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Carts)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Cart_Accounts");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B2AD3376BBB");

            entity.Property(e => e.CartItemId).HasColumnName("CartItemID");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.PriceAtThatTime).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItems_Cart");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Products");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B0F5FC8EC");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E0FE7CB821").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SuperCategoryId).HasColumnName("SuperCategoryID");

            entity.HasOne(d => d.SuperCategory).WithMany(p => p.Categories)
                .HasForeignKey(d => d.SuperCategoryId)
                .HasConstraintName("FK_Categories_SuperCategories");
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.EmailOtpId).HasName("PK__EmailOtp__24FA0B1562D3E0A6");

            entity.Property(e => e.EmailOtpId).HasColumnName("EmailOtpID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.OtpCode).HasMaxLength(10);
            entity.Property(e => e.Purpose).HasMaxLength(50);

            entity.HasOne(d => d.Account).WithMany(p => p.EmailOtps)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmailOtps_Accounts");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.ExternalLoginId).HasName("PK__External__A8FDB38E6E882DAA");

            entity.HasIndex(e => new { e.Provider, e.ProviderKey }, "UQ_ExternalLogins").IsUnique();

            entity.Property(e => e.ExternalLoginId).HasColumnName("ExternalLoginID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.ProviderKey).HasMaxLength(255);

            entity.HasOne(d => d.Account).WithMany(p => p.ExternalLogins)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_ExternalLogins_Accounts");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C50613171D3226F3");

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaterialName).HasMaxLength(255);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E32AE7E7CDF");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.ConditionJson).HasColumnName("ConditionJSON");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.ScheduledAt).HasColumnType("datetime");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.TargetRoleId).HasColumnName("TargetRoleID");
            entity.Property(e => e.TargetType).HasMaxLength(50);
            entity.Property(e => e.TargetUserId).HasColumnName("TargetUserID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValue("General");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__Creat__2D12A970");

            entity.HasOne(d => d.TargetRole).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TargetRoleId)
                .HasConstraintName("FK__Notificat__Targe__2B2A60FE");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.NotificationTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .HasConstraintName("FK__Notificat__Targe__2C1E8537");
        });

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Notifica__5E5499A848753643");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.Result)
                .HasMaxLength(50)
                .HasDefaultValue("Success");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SentTo).HasMaxLength(255);

            entity.HasOne(d => d.Notification).WithMany(p => p.NotificationLogs)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK__Notificat__Notif__379037E3");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF0FCB0506");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaidByExternalAmount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.PaidByWalletAmount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.PaymentCompletedAt).HasColumnType("datetime");
            entity.Property(e => e.RefundStatus)
                .HasMaxLength(15)
                .HasDefaultValue("None");
            entity.Property(e => e.ShippingAddressLine).HasMaxLength(500);
            entity.Property(e => e.ShippingCity).HasMaxLength(100);
            entity.Property(e => e.ShippingMethod).HasMaxLength(100);
            entity.Property(e => e.ShippingName).HasMaxLength(100);
            entity.Property(e => e.ShippingPhone).HasMaxLength(20);
            entity.Property(e => e.ShippingWard).HasMaxLength(100);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Accounts");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_StatusOrder");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C9E99D9C8");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Discount).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Total)
                .HasComputedColumnSql("(([Quantity]*[UnitPrice])*((1)-[Discount]/(100.0)))", true)
                .HasColumnType("numeric(36, 9)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Products");
        });

        modelBuilder.Entity<OrderRefund>(entity =>
        {
            entity.HasKey(e => e.RefundId).HasName("PK__OrderRef__725AB9005B3AC5E8");

            entity.HasIndex(e => e.OrderId, "UX_OrderRefunds_OnePerOrder").IsUnique();

            entity.Property(e => e.RefundId).HasColumnName("RefundID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ApprovedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.RefundMode)
                .HasMaxLength(10)
                .HasDefaultValue("Wallet");
            entity.Property(e => e.RefundStatus)
                .HasMaxLength(12)
                .HasDefaultValue("Requested");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");

            entity.HasOne(d => d.Account).WithMany(p => p.OrderRefundAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Refunds_Accounts");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.OrderRefundApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Refunds_ApprovedBy");

            entity.HasOne(d => d.Order).WithOne(p => p.OrderRefund)
                .HasForeignKey<OrderRefund>(d => d.OrderId)
                .HasConstraintName("FK_Refunds_Orders");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.OrderRefundRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .HasConstraintName("FK_Refunds_RequestedBy");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.OrderRefunds)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_Refunds_WalletTxn");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.OrderStatusHistoryId).HasName("PK__OrderSta__D16EDBA32658C6A6");

            entity.ToTable("OrderStatusHistory");

            entity.Property(e => e.OrderStatusHistoryId).HasColumnName("OrderStatusHistoryID");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK_OSH_ChangedBy");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OSH_Orders");

            entity.HasOne(d => d.Status).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_OSH_StatusOrder");
        });

        modelBuilder.Entity<Origin>(entity =>
        {
            entity.HasKey(e => e.OriginId).HasName("PK__Origins__171FA2C6772F7578");

            entity.HasIndex(e => e.OriginName, "UQ__Origins__636F5CFDB125BC80").IsUnique();

            entity.Property(e => e.OriginId).HasColumnName("OriginID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OriginName).HasMaxLength(255);
        });

        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.HasKey(e => e.PaymentHistoryId).HasName("PK__PaymentH__F3B933911C6D6A42");

            entity.ToTable("PaymentHistory");

            entity.Property(e => e.PaymentHistoryId).HasColumnName("PaymentHistoryID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("VND");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Failed");
            entity.Property(e => e.TransactionCode).HasMaxLength(100);
            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");

            entity.HasOne(d => d.Account).WithMany(p => p.PaymentHistories)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentHistory_Accounts");

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_PaymentHistory_Orders");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.PaymentHistories)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_PaymentHistory_WalletTxn");
        });

        modelBuilder.Entity<PriceRange>(entity =>
        {
            entity.HasKey(e => e.PriceRangeId).HasName("PK__PriceRan__B8A301FF739FFEFA");

            entity.Property(e => e.PriceRangeId).HasColumnName("PriceRangeID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PriceRangeMax).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PriceRangeMin).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED6FD5F26C");

            entity.HasIndex(e => e.Sku, "UQ__Products__CA1ECF0D1741ED56").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.AgeId).HasColumnName("AgeID");
            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.OriginId).HasColumnName("OriginID");
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PriceRangeId).HasColumnName("PriceRangeID");
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(15)
                .HasDefaultValue("Available");
            entity.Property(e => e.SexId).HasColumnName("SexID");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SKU");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Age).WithMany(p => p.Products)
                .HasForeignKey(d => d.AgeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Ages");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Brands");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Categories");

            entity.HasOne(d => d.Material).WithMany(p => p.Products)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Materials");

            entity.HasOne(d => d.Origin).WithMany(p => p.Products)
                .HasForeignKey(d => d.OriginId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Origins");

            entity.HasOne(d => d.PriceRange).WithMany(p => p.Products)
                .HasForeignKey(d => d.PriceRangeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_PriceRanges");

            entity.HasOne(d => d.Sex).WithMany(p => p.Products)
                .HasForeignKey(d => d.SexId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Products_Sexes");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F4EC13108E1D");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductImages_Products");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42F2F67DF4377");

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(15);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Promotions_Products");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("PK__RefreshT__F5845E59ECBF82DC");

            entity.Property(e => e.RefreshTokenId).HasColumnName("RefreshTokenID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(500);

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_RefreshTokens_Accounts");
        });

        modelBuilder.Entity<ReviewBlog>(entity =>
        {
            entity.HasKey(e => e.ReviewBlogId).HasName("PK__ReviewBl__A19536C00DF81AA5");

            entity.HasIndex(e => new { e.AccountId, e.BlogPostId }, "UQ_ReviewBlogs_PostAccount").IsUnique();

            entity.Property(e => e.ReviewBlogId).HasColumnName("ReviewBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogs)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_ReviewBlogs_Accounts");

            entity.HasOne(d => d.BlogPost).WithMany(p => p.ReviewBlogs)
                .HasForeignKey(d => d.BlogPostId)
                .HasConstraintName("FK_ReviewBlogs_Posts");
        });

        modelBuilder.Entity<ReviewBlogReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionBlogId).HasName("PK__ReviewBl__6A8A0D27B85DA4EC");

            entity.HasIndex(e => new { e.ReviewBlogId, e.AccountId }, "UQ_ReviewBlogReactions").IsUnique();

            entity.Property(e => e.ReactionBlogId).HasColumnName("ReactionBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReactionType).HasMaxLength(10);
            entity.Property(e => e.ReviewBlogId).HasColumnName("ReviewBlogID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReactions_Accounts");

            entity.HasOne(d => d.ReviewBlog).WithMany(p => p.ReviewBlogReactions)
                .HasForeignKey(d => d.ReviewBlogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReactions_ReviewBlogs");
        });

        modelBuilder.Entity<ReviewBlogReply>(entity =>
        {
            entity.HasKey(e => e.ReplyBlogId).HasName("PK__ReviewBl__59963641039BE472");

            entity.Property(e => e.ReplyBlogId).HasColumnName("ReplyBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReviewBlogId).HasColumnName("ReviewBlogID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogReplies)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplies_Accounts");

            entity.HasOne(d => d.ReviewBlog).WithMany(p => p.ReviewBlogReplies)
                .HasForeignKey(d => d.ReviewBlogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplies_ReviewBlogs");
        });

        modelBuilder.Entity<ReviewProduct>(entity =>
        {
            entity.HasKey(e => e.ReviewProductId).HasName("PK__ReviewPr__02A6803A95EC4C9C");

            entity.HasIndex(e => new { e.AccountId, e.ProductId }, "UQ_Reviews_ProductAccount").IsUnique();

            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProducts)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Reviews_Accounts");

            entity.HasOne(d => d.Product).WithMany(p => p.ReviewProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Products");
        });

        modelBuilder.Entity<ReviewProductImage>(entity =>
        {
            entity.HasKey(e => e.ReviewProductImageId).HasName("PK__ReviewPr__013E0F1E32C67B29");

            entity.Property(e => e.ReviewProductImageId).HasColumnName("ReviewProductImageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductImages)
                .HasForeignKey(d => d.ReviewProductId)
                .HasConstraintName("FK_ReviewProdImages_ReviewProducts");
        });

        modelBuilder.Entity<ReviewProductReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionProductId).HasName("PK__ReviewPr__B56FCAF104799FC1");

            entity.HasIndex(e => new { e.ReviewProductId, e.AccountId }, "UQ_ReviewProductReactions").IsUnique();

            entity.Property(e => e.ReactionProductId).HasColumnName("ReactionProductID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReactionType).HasMaxLength(10);
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProdReactions_Accounts");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProdReactions_Reviews");
        });

        modelBuilder.Entity<ReviewProductReply>(entity =>
        {
            entity.HasKey(e => e.ReplyProductId).HasName("PK__ReviewPr__2DCE233CAB8CCAE2");

            entity.Property(e => e.ReplyProductId).HasColumnName("ReplyProductID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProductReplies)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProdReplies_Accounts");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductReplies)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProdReplies_Reviews");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A90D5FCE3");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61607A4BCA86").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Sex>(entity =>
        {
            entity.HasKey(e => e.SexId).HasName("PK__Sexes__75622DB69E292150");

            entity.HasIndex(e => e.SexName, "UQ__Sexes__BA354290495CBABF").IsUnique();

            entity.Property(e => e.SexId).HasColumnName("SexID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SexName).HasMaxLength(20);
        });

        modelBuilder.Entity<StatusOrder>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__StatusOr__C8EE20437AFC5EE6");

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
        });

        modelBuilder.Entity<SuperCategory>(entity =>
        {
            entity.HasKey(e => e.SuperCategoryId).HasName("PK__SuperCat__CEB990D35593A8D8");

            entity.HasIndex(e => e.SuperCategoryName, "UQ__SuperCat__3FA779DFAA2CAFE1").IsUnique();

            entity.Property(e => e.SuperCategoryId).HasColumnName("SuperCategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SuperCategoryName).HasMaxLength(255);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.UserNotificationId).HasName("PK__UserNoti__EB2985C93328BB89");

            entity.Property(e => e.UserNotificationId).HasColumnName("UserNotificationID");
            entity.Property(e => e.DeliveredAt).HasColumnType("datetime");
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Notification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK__UserNotif__Notif__30E33A54");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserNotif__UserI__31D75E8D");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE79C11F6B3FF2");

            entity.HasIndex(e => e.VoucherCode, "UQ__Vouchers__7F0ABCA9BB75F3BE").IsUnique();

            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.VoucherCode).HasMaxLength(50);
            entity.Property(e => e.VoucherTypeId).HasColumnName("VoucherTypeID");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vouchers_Accounts");

            entity.HasOne(d => d.VoucherType).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.VoucherTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vouchers_VoucherTypes");
        });

        modelBuilder.Entity<VoucherType>(entity =>
        {
            entity.HasKey(e => e.VoucherTypeId).HasName("PK__VoucherT__6541283DA0DA46AE");

            entity.Property(e => e.VoucherTypeId).HasColumnName("VoucherTypeID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VoucherTypeName).HasMaxLength(255);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92EA40FD7C7");

            entity.HasIndex(e => e.AccountId, "UQ__Wallets__349DA58718AAA2FB").IsUnique();

            entity.Property(e => e.WalletId).HasColumnName("WalletID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Balance).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("VND");
            entity.Property(e => e.LastTransactionAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.AccountId)
                .HasConstraintName("FK_Wallets_Accounts");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.WalletTransactionId).HasName("PK__WalletTr__7184AECF39C32A09");

            entity.HasIndex(e => e.RelatedOrderId, "IX_WalletTxn_Order");

            entity.HasIndex(e => new { e.WalletId, e.CreatedAt }, "IX_WalletTxn_Wallet_CreatedAt").IsDescending(false, true);

            entity.HasIndex(e => e.IdempotencyKey, "UX_WalletTxn_Idempotency")
                .IsUnique()
                .HasFilter("([IdempotencyKey] IS NOT NULL)");

            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.BalanceAfter).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.BalanceBefore).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Direction).HasMaxLength(3);
            entity.Property(e => e.ExternalRef).HasMaxLength(100);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.Property(e => e.Method).HasMaxLength(30);
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.RelatedOrderId).HasColumnName("RelatedOrderID");
            entity.Property(e => e.RelatedPaymentHistoryId).HasColumnName("RelatedPaymentHistoryID");
            entity.Property(e => e.Status)
                .HasMaxLength(12)
                .HasDefaultValue("Completed");
            entity.Property(e => e.TxnType).HasMaxLength(20);
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Account).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTxn_Accounts");

            entity.HasOne(d => d.RelatedOrder).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.RelatedOrderId)
                .HasConstraintName("FK_WalletTxn_Orders");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WalletId)
                .HasConstraintName("FK_WalletTxn_Wallets");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__Wishlist__233189CBE9A2F9BA");

            entity.HasIndex(e => new { e.AccountId, e.ProductId }, "UQ_Wishlists").IsUnique();

            entity.Property(e => e.WishlistId).HasColumnName("WishlistID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Wishlists_Accounts");

            entity.HasOne(d => d.Product).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Wishlists_Products");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
