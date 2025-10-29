using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public int? RoleId { get; set; }

    public string AccountName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = null!;

    public string? Image { get; set; }

    public string Password { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Provider { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<EmailOtp> EmailOtps { get; set; } = new List<EmailOtp>();

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationTargetUsers { get; set; } = new List<Notification>();

    public virtual ICollection<OrderRefund> OrderRefundAccounts { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderRefund> OrderRefundApprovedByNavigations { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderRefund> OrderRefundRequestedByNavigations { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

    public virtual ICollection<ReviewBlogReaction> ReviewBlogReactions { get; set; } = new List<ReviewBlogReaction>();

    public virtual ICollection<ReviewBlogReply> ReviewBlogReplies { get; set; } = new List<ReviewBlogReply>();

    public virtual ICollection<ReviewBlog> ReviewBlogs { get; set; } = new List<ReviewBlog>();

    public virtual ICollection<ReviewProductReaction> ReviewProductReactions { get; set; } = new List<ReviewProductReaction>();

    public virtual ICollection<ReviewProductReply> ReviewProductReplies { get; set; } = new List<ReviewProductReply>();

    public virtual ICollection<ReviewProduct> ReviewProducts { get; set; } = new List<ReviewProduct>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    public virtual Wallet? Wallet { get; set; }

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
