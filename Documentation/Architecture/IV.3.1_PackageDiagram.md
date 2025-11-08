# IV.3.1. Package Diagram - ASP_LorKingDom E-Commerce System

## I. Giới thiệu về Package Diagram

**Package Diagram** là một loại biểu đồ UML được sử dụng để hiển thị cấu trúc tổ chức của hệ thống thành các packages (gói) theo cách phân cấp. Nó giúp:
- Tổ chức các components thành các nhóm có ý nghĩa
- Hiển thị các phụ thuộc giữa các packages
- Cung cấp cái nhìn tổng quan về kiến trúc hệ thống
- Dễ dàng quản lý và bảo trì các phần của hệ thống

---

## II. Package Diagram - ASP_LorKingDom System

### A. Biểu đồ Toàn Bộ Hệ Thống

```
╔════════════════════════════════════════════════════════════════════════════════════════╗
║                          ASP_LorKingDom.System                                        ║
║                    (E-Commerce Platform - 3-Tier Architecture)                        ║
╠════════════════════════════════════════════════════════════════════════════════════════╣
║                                                                                        ║
║   ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓     ║
║   ┃ ┌─ WebUI (Presentation Layer)                                           ┃     ║
║   ┃ │  <<ASP.NET Core MVC Module>>                                           ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ WebUI.Controllers                                           │     ┃     ║
║   ┃ │  │ ┌─ AccountCustomerController                              │     ┃     ║
║   ┃ │  │ ├─ AccountStaffController                                │     ┃     ║
║   ┃ │  │ ├─ AdminAuthController                                  │     ┃     ║
║   ┃ │  │ ├─ AdminController                                      │     ┃     ║
║   ┃ │  │ ├─ AuthController                                       │     ┃     ║
║   ┃ │  │ ├─ ProductController                                    │     ┃     ║
║   ┃ │  │ ├─ CategoryController                                   │     ┃     ║
║   ┃ │  │ ├─ CartController                                       │     ┃     ║
║   ┃ │  │ ├─ OrderController                                      │     ┃     ║
║   ┃ │  │ ├─ OrderRefundAdminController                           │     ┃     ║
║   ┃ │  │ ├─ PaymentController (via BLL Services)                 │     ┃     ║
║   ┃ │  │ ├─ BlogController                                       │     ┃     ║
║   ┃ │  │ ├─ BlogCategoryController                               │     ┃     ║
║   ┃ │  │ ├─ BlogReviewController                                 │     ┃     ║
║   ┃ │  │ ├─ ReviewController                                     │     ┃     ║
║   ┃ │  │ ├─ ReviewProductController                              │     ┃     ║
║   ┃ │  │ ├─ WalletController                                     │     ┃     ║
║   ┃ │  │ ├─ ChatController                                       │     ┃     ║
║   ┃ │  │ ├─ ChatDashboardApiController                           │     ┃     ║
║   ┃ │  │ ├─ MyNotificationsController                            │     ┃     ║
║   ┃ │  │ ├─ NotificationController                               │     ┃     ║
║   ┃ │  │ ├─ VoucherController                                    │     ┃     ║
║   ┃ │  │ ├─ PromotionController                                  │     ┃     ║
║   ┃ │  │ ├─ WishlistController                                   │     ┃     ║
║   ┃ │  │ ├─ BrandController                                      │     ┃     ║
║   ┃ │  │ ├─ MaterialController                                   │     ┃     ║
║   ┃ │  │ ├─ OriginController                                     │     ┃     ║
║   ┃ │  │ ├─ PriceRangeController                                 │     ┃     ║
║   ┃ │  │ ├─ SuperCategoryController                              │     ┃     ║
║   ┃ │  │ ├─ AddressController                                    │     ┃     ║
║   ┃ │  │ ├─ StatisticsController                                 │     ┃     ║
║   ┃ │  │ └─ HomeController                                       │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ WebUI.Views (Razor Templates)                              │     ┃     ║
║   ┃ │  │ ┌─ Admin/                                                 │     ┃     ║
║   ┃ │  │ ├─ AdminAuth/                                             │     ┃     ║
║   ┃ │  │ ├─ Auth/                                                  │     ┃     ║
║   ┃ │  │ ├─ Blog/                                                  │     ┃     ║
║   ┃ │  │ ├─ Chat/                                                  │     ┃     ║
║   ┃ │  │ ├─ Home/                                                  │     ┃     ║
║   ┃ │  │ ├─ MyNotifications/                                       │     ┃     ║
║   ┃ │  │ ├─ Review/                                                │     ┃     ║
║   ┃ │  │ ├─ Shared/ (_ViewImports.cshtml, _ViewStart.cshtml)      │     ┃     ║
║   ┃ │  │ ├─ Wallet/                                                │     ┃     ║
║   ┃ │  │ └─ Wishlist/                                              │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ WebUI.ChatHubs                                              │     ┃     ║
║   ┃ │  │ ┌─ ChatHub (SignalR Hub)                                   │     ┃     ║
║   ┃ │  │ │  ├─ SendMessage()                                        │     ┃     ║
║   ┃ │  │ │  ├─ JoinGroup()                                          │     ┃     ║
║   ┃ │  │ │  ├─ LeaveGroup()                                         │     ┃     ║
║   ┃ │  │ │  └─ ReceiveMessage()                                     │     ┃     ║
║   ┃ │  │ └─ <<signal-r>> signalR communication protocol             │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ WebUI.Filters                                               │     ┃     ║
║   ┃ │  │ ┌─ AdminAuthorizationAttributes                            │     ┃     ║
║   ┃ │  │ │  └─ Validates role-based access for admin routes        │     ┃     ║
║   ┃ │  │ └─ <<authorization-filter>>                                │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ WebUI.Workers (Background Services)                        │     ┃     ║
║   ┃ │  │ ┌─ NotificationWorkerService                              │     ┃     ║
║   ┃ │  │ │  └─ Executes background notification tasks              │     ┃     ║
║   ┃ │  │ ├─ PromotionWorkerService                                 │     ┃     ║
║   ┃ │  │ │  └─ Executes background promotion tasks                 │     ┃     ║
║   ┃ │  │ └─ <<background-service>> IHostedService                  │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ wwwroot (Static Assets)                                     │     ┃     ║
║   ┃ │  │ ├─ css/                                                    │     ┃     ║
║   ┃ │  │ ├─ js/                                                     │     ┃     ║
║   ┃ │  │ ├─ lib/                                                    │     ┃     ║
║   ┃ │  │ ├─ uploads/                                                │     ┃     ║
║   ┃ │  │ └─ Assets/                                                 │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ └────────────────────────────────────────────────────────────────┘     ║
║   ┃                              ▼ <<import>>                              ║
║   ┃                           <<access>>                                   ║
║   ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛     ║
║                                                                                        ║
║   ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓     ║
║   ┃ ┌─ BLL (Business Logic Layer)                                          ┃     ║
║   ┃ │  <<Business Logic Module>>                                            ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ BLL.Services (Business Logic Implementations)               │     ┃     ║
║   ┃ │  │ ┌─ AccountService                                          │     ┃     ║
║   ┃ │  │ ├─ ProductService                                          │     ┃     ║
║   ┃ │  │ ├─ OrderService                                            │     ┃     ║
║   ┃ │  │ ├─ CartService                                             │     ┃     ║
║   ┃ │  │ ├─ PaymentService                                          │     ┃     ║
║   ┃ │  │ │  ├─ VNPay Payment Processing                             │     ┃     ║
║   ┃ │  │ │  ├─ Momo Payment Processing                              │     ┃     ║
║   ┃ │  │ │  └─ COD Payment Processing                               │     ┃     ║
║   ┃ │  │ ├─ BlogService                                             │     ┃     ║
║   ┃ │  │ ├─ ReviewService                                           │     ┃     ║
║   ┃ │  │ ├─ WalletService                                           │     ┃     ║
║   ┃ │  │ ├─ PromotionService                                        │     ┃     ║
║   ┃ │  │ ├─ VoucherService                                          │     ┃     ║
║   ┃ │  │ ├─ ChatService                                             │     ┃     ║
║   ┃ │  │ ├─ NotificationService                                     │     ┃     ║
║   ┃ │  │ ├─ RefundService                                           │     ┃     ║
║   ┃ │  │ ├─ StatisticsService                                       │     ┃     ║
║   ┃ │  │ ├─ CategoryService                                         │     ┃     ║
║   ┃ │  │ ├─ BrandService                                            │     ┃     ║
║   ┃ │  │ ├─ MaterialService                                         │     ┃     ║
║   ┃ │  │ ├─ OriginService                                           │     ┃     ║
║   ┃ │  │ ├─ AddressService                                          │     ┃     ║
║   ┃ │  │ ├─ WishlistService                                         │     ┃     ║
║   ┃ │  │ └─ SuperCategoryService                                    │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ BLL.Interfaces (Service Contracts)                         │     ┃     ║
║   ┃ │  │ ┌─ IAccountService                                         │     ┃     ║
║   ┃ │  │ ├─ IProductService                                         │     ┃     ║
║   ┃ │  │ ├─ IOrderService                                           │     ┃     ║
║   ┃ │  │ ├─ ICartService                                            │     ┃     ║
║   ┃ │  │ ├─ IPaymentService                                         │     ┃     ║
║   ┃ │  │ ├─ IBlogService                                            │     ┃     ║
║   ┃ │  │ ├─ IReviewService                                          │     ┃     ║
║   ┃ │  │ ├─ IWalletService                                          │     ┃     ║
║   ┃ │  │ ├─ IPromotionService                                       │     ┃     ║
║   ┃ │  │ ├─ IVoucherService                                         │     ┃     ║
║   ┃ │  │ ├─ IChatService                                            │     ┃     ║
║   ┃ │  │ ├─ INotificationService                                    │     ┃     ║
║   ┃ │  │ ├─ IRefundService                                          │     ┃     ║
║   ┃ │  │ ├─ IStatisticsService                                      │     ┃     ║
║   ┃ │  │ ├─ ICategoryService                                        │     ┃     ║
║   ┃ │  │ ├─ IBrandService                                           │     ┃     ║
║   ┃ │  │ ├─ IMaterialService                                        │     ┃     ║
║   ┃ │  │ ├─ IOriginService                                          │     ┃     ║
║   ┃ │  │ ├─ IAddressService                                         │     ┃     ║
║   ┃ │  │ ├─ IWishlistService                                        │     ┃     ║
║   ┃ │  │ └─ ISuperCategoryService                                   │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ BLL.DTOs (Data Transfer Objects)                            │     ┃     ║
║   ┃ │  │ ┌─ AccountDto                                              │     ┃     ║
║   ┃ │  │ ├─ ProductDto                                              │     ┃     ║
║   ┃ │  │ ├─ OrderDto                                                │     ┃     ║
║   ┃ │  │ ├─ CartDto                                                 │     ┃     ║
║   ┃ │  │ ├─ BlogDto                                                 │     ┃     ║
║   ┃ │  │ ├─ ReviewDto                                               │     ┃     ║
║   ┃ │  │ ├─ WalletDto                                               │     ┃     ║
║   ┃ │  │ ├─ ChatDto                                                 │     ┃     ║
║   ┃ │  │ ├─ NotificationDto                                         │     ┃     ║
║   ┃ │  │ ├─ VoucherDto                                              │     ┃     ║
║   ┃ │  │ ├─ PromotionDto                                            │     ┃     ║
║   ┃ │  │ ├─ RefundDto                                               │     ┃     ║
║   ┃ │  │ ├─ CategoryDto                                             │     ┃     ║
║   ┃ │  │ ├─ BrandDto                                                │     ┃     ║
║   ┃ │  │ ├─ MaterialDto                                             │     ┃     ║
║   ┃ │  │ ├─ OriginDto                                               │     ┃     ║
║   ┃ │  │ ├─ AddressDto                                              │     ┃     ║
║   ┃ │  │ ├─ WishlistDto                                             │     ┃     ║
║   ┃ │  │ └─ StatisticsDto                                           │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ BLL.Validators (Input Validation)                          │     ┃     ║
║   ┃ │  │ ┌─ AccountValidator                                        │     ┃     ║
║   ┃ │  │ ├─ ProductValidator                                        │     ┃     ║
║   ┃ │  │ ├─ OrderValidator                                          │     ┃     ║
║   ┃ │  │ ├─ CartValidator                                           │     ┃     ║
║   ┃ │  │ ├─ BlogValidator                                           │     ┃     ║
║   ┃ │  │ ├─ ReviewValidator                                         │     ┃     ║
║   ┃ │  │ ├─ PaymentValidator                                        │     ┃     ║
║   ┃ │  │ └─ <<fluent-validation>> Using FluentValidation Framework  │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ └────────────────────────────────────────────────────────────────┘     ║
║   ┃                              ▼ <<import>>                              ║
║   ┃                           <<access>>                                   ║
║   ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛     ║
║                                                                                        ║
║   ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓     ║
║   ┃ ┌─ DAL (Data Access Layer)                                            ┃     ║
║   ┃ │  <<Data Persistence Module>>                                         ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ DAL.Repositories (Data Access Implementations)              │     ┃     ║
║   ┃ │  │ ┌─ GenericRepository<T>                                    │     ┃     ║
║   ┃ │  │ │  ├─ CRUD Operations (Create, Read, Update, Delete)       │     ┃     ║
║   ┃ │  │ │  ├─ Pagination, Filtering, Sorting                       │     ┃     ║
║   ┃ │  │ │  └─ Query Operations                                     │     ┃     ║
║   ┃ │  │ ├─ AccountRepository                                        │     ┃     ║
║   ┃ │  │ ├─ ProductRepository                                        │     ┃     ║
║   ┃ │  │ ├─ OrderRepository                                          │     ┃     ║
║   ┃ │  │ ├─ OrderDetailRepository                                    │     ┃     ║
║   ┃ │  │ ├─ CartRepository                                           │     ┃     ║
║   ┃ │  │ ├─ CartItemRepository                                       │     ┃     ║
║   ┃ │  │ ├─ CategoryRepository                                       │     ┃     ║
║   ┃ │  │ ├─ SuperCategoryRepository                                  │     ┃     ║
║   ┃ │  │ ├─ BrandRepository                                          │     ┃     ║
║   ┃ │  │ ├─ MaterialRepository                                       │     ┃     ║
║   ┃ │  │ ├─ OriginRepository                                         │     ┃     ║
║   ┃ │  │ ├─ BlogRepository                                           │     ┃     ║
║   ┃ │  │ ├─ BlogCategoryRepository                                   │     ┃     ║
║   ┃ │  │ ├─ ReviewRepository                                         │     ┃     ║
║   ┃ │  │ ├─ WalletRepository                                         │     ┃     ║
║   ┃ │  │ ├─ TransactionRepository                                    │     ┃     ║
║   ┃ │  │ ├─ PromotionRepository                                      │     ┃     ║
║   ┃ │  │ ├─ VoucherRepository                                        │     ┃     ║
║   ┃ │  │ ├─ ChatRepository                                           │     ┃     ║
║   ┃ │  │ ├─ MessageRepository                                        │     ┃     ║
║   ┃ │  │ ├─ NotificationRepository                                   │     ┃     ║
║   ┃ │  │ ├─ RefundRequestRepository                                  │     ┃     ║
║   ┃ │  │ ├─ WishlistRepository                                       │     ┃     ║
║   ┃ │  │ └─ AddressRepository                                        │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ DAL.Interfaces (Repository Contracts)                      │     ┃     ║
║   ┃ │  │ ┌─ IGenericRepository<T>                                   │     ┃     ║
║   ┃ │  │ ├─ IAccountRepository                                       │     ┃     ║
║   ┃ │  │ ├─ IProductRepository                                       │     ┃     ║
║   ┃ │  │ ├─ IOrderRepository                                         │     ┃     ║
║   ┃ │  │ ├─ ICartRepository                                          │     ┃     ║
║   ┃ │  │ ├─ ICategoryRepository                                      │     ┃     ║
║   ┃ │  │ ├─ IBlogRepository                                          │     ┃     ║
║   ┃ │  │ ├─ IReviewRepository                                        │     ┃     ║
║   ┃ │  │ ├─ IWalletRepository                                        │     ┃     ║
║   ┃ │  │ ├─ IPromotionRepository                                     │     ┃     ║
║   ┃ │  │ ├─ IVoucherRepository                                       │     ┃     ║
║   ┃ │  │ ├─ IChatRepository                                          │     ┃     ║
║   ┃ │  │ ├─ INotificationRepository                                  │     ┃     ║
║   ┃ │  │ ├─ IRefundRequestRepository                                 │     ┃     ║
║   ┃ │  │ ├─ IBrandRepository                                         │     ┃     ║
║   ┃ │  │ ├─ IMaterialRepository                                      │     ┃     ║
║   ┃ │  │ ├─ IOriginRepository                                        │     ┃     ║
║   ┃ │  │ ├─ IWishlistRepository                                      │     ┃     ║
║   ┃ │  │ ├─ IAddressRepository                                       │     ┃     ║
║   ┃ │  │ └─ ISuperCategoryRepository                                 │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ DAL.Models (Entity Models)                                 │     ┃     ║
║   ┃ │  │ ┌─ Account                                                 │     ┃     ║
║   ┃ │  │ ├─ Product                                                 │     ┃     ║
║   ┃ │  │ ├─ Order                                                   │     ┃     ║
║   ┃ │  │ ├─ OrderDetail                                             │     ┃     ║
║   ┃ │  │ ├─ Cart                                                    │     ┃     ║
║   ┃ │  │ ├─ CartItem                                                │     ┃     ║
║   ┃ │  │ ├─ Category                                                │     ┃     ║
║   ┃ │  │ ├─ SuperCategory                                           │     ┃     ║
║   ┃ │  │ ├─ Brand                                                   │     ┃     ║
║   ┃ │  │ ├─ Material                                                │     ┃     ║
║   ┃ │  │ ├─ Origin                                                  │     ┃     ║
║   ┃ │  │ ├─ Blog                                                    │     ┃     ║
║   ┃ │  │ ├─ BlogCategory                                            │     ┃     ║
║   ┃ │  │ ├─ Review                                                  │     ┃     ║
║   ┃ │  │ ├─ Wallet                                                  │     ┃     ║
║   ┃ │  │ ├─ Transaction                                             │     ┃     ║
║   ┃ │  │ ├─ Promotion                                               │     ┃     ║
║   ┃ │  │ ├─ Voucher                                                 │     ┃     ║
║   ┃ │  │ ├─ Chat                                                    │     ┃     ║
║   ┃ │  │ ├─ Message                                                 │     ┃     ║
║   ┃ │  │ ├─ Notification                                            │     ┃     ║
║   ┃ │  │ ├─ RefundRequest                                           │     ┃     ║
║   ┃ │  │ ├─ Wishlist                                                │     ┃     ║
║   ┃ │  │ └─ Address                                                 │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌─────────────────────────────────────────────────────────────┐     ┃     ║
║   ┃ │  │ DAL.DbContext                                              │     ┃     ║
║   ┃ │  │ ┌─ LorKingdomDbContext                                    │     ┃     ║
║   ┃ │  │ │  ├─ Entity Framework Core DbContext                      │     ┃     ║
║   ┃ │  │ │  ├─ Database Configuration                               │     ┃     ║
║   ┃ │  │ │  ├─ Relationship Configurations                          │     ┃     ║
║   ┃ │  │ │  ├─ Data Seeding                                         │     ┃     ║
║   ┃ │  │ │  └─ Migration Support                                    │     ┃     ║
║   ┃ │  │ └─ <<entity-framework>>                                    │     ┃     ║
║   ┃ │  └─────────────────────────────────────────────────────────────┘     ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ └────────────────────────────────────────────────────────────────┘     ║
║   ┃                                                                          ║
║   ┃  ┌──────────────────────────────────────────────────────────────────┐  ║
║   ┃  │ Database (SQL Server)                                           │  ║
║   ┃  │ <<persistent-storage>>                                          │  ║
║   ┃  │                                                                  │  ║
║   ┃  │ Tables: Accounts, Products, Orders, Carts, Categories,          │  ║
║   ┃  │         Blogs, Reviews, Wallets, Transactions, Promotions,      │  ║
║   ┃  │         Vouchers, Chats, Messages, Notifications,               │  ║
║   ┃  │         RefundRequests, Wishlists, Addresses, Brands,           │  ║
║   ┃  │         Materials, Origins                                      │  ║
║   ┃  └──────────────────────────────────────────────────────────────────┘  ║
║   ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛     ║
║                                                                                        ║
║   ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓     ║
║   ┃ ┌─ External Services & Integrations                                     ┃     ║
║   ┃ │  <<external-services>>                                                 ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌──────────────────────────────────────────────────────────────┐    ┃     ║
║   ┃ │  │ Payment Gateway Services                                    │    ┃     ║
║   ┃ │  │ ┌─ VNPay Payment Gateway                                   │    ┃     ║
║   ┃ │  │ │  ├─ Create Payment URL                                   │    ┃     ║
║   ┃ │  │ │  ├─ Verify Payment                                       │    ┃     ║
║   ┃ │  │ │  └─ Query Transaction Status                             │    ┃     ║
║   ┃ │  │ ├─ Momo Payment Gateway                                    │    ┃     ║
║   ┃ │  │ │  ├─ Create Payment URL                                   │    ┃     ║
║   ┃ │  │ │  ├─ Verify Payment                                       │    ┃     ║
║   ┃ │  │ │  └─ Query Transaction Status                             │    ┃     ║
║   ┃ │  │ └─ <<payment-gateway>>                                     │    ┃     ║
║   ┃ │  └──────────────────────────────────────────────────────────────┘    ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌──────────────────────────────────────────────────────────────┐    ┃     ║
║   ┃ │  │ Email Service                                              │    ┃     ║
║   ┃ │  │ ┌─ SMTP Configuration                                      │    ┃     ║
║   ┃ │  │ ├─ Send Email Notifications                                │    ┃     ║
║   ┃ │  │ ├─ Order Confirmation                                      │    ┃     ║
║   ┃ │  │ ├─ Password Reset                                          │    ┃     ║
║   ┃ │  │ └─ Promotion Updates                                       │    ┃     ║
║   ┃ │  └──────────────────────────────────────────────────────────────┘    ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ │  ┌──────────────────────────────────────────────────────────────┐    ┃     ║
║   ┃ │  │ File Storage Service                                        │    ┃     ║
║   ┃ │  │ ┌─ Upload Product Images                                   │    ┃     ║
║   ┃ │  │ ├─ Upload User Avatars                                     │    ┃     ║
║   ┃ │  │ ├─ Upload Blog Images                                      │    ┃     ║
║   ┃ │  │ └─ Local wwwroot/uploads/                                  │    ┃     ║
║   ┃ │  └──────────────────────────────────────────────────────────────┘    ┃     ║
║   ┃ │                                                                        ┃     ║
║   ┃ └────────────────────────────────────────────────────────────────┘     ║
║   ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛     ║
║                                                                                        ║
╚════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## III. Chi Tiết Phụ Thuộc (Dependencies)

### A. Phụ Thuộc Chính (Primary Dependencies)

```
WebUI
  │
  ├─── <<import>> ──→ BLL.Services
  │
  ├─── <<access>> ──→ BLL.DTOs
  │
  ├─── <<import>> ──→ BLL.Interfaces
  │
  └─── <<access>> ──→ BLL.Validators


BLL (Business Logic Layer)
  │
  ├─── <<import>> ──→ DAL.Repositories
  │
  ├─── <<access>> ──→ DAL.Models
  │
  ├─── <<import>> ──→ DAL.Interfaces
  │
  └─── <<access>> ──→ External Services
      │
      ├─ Payment Gateway (VNPay, Momo)
      ├─ Email Service
      └─ File Storage Service


DAL (Data Access Layer)
  │
  ├─── <<import>> ──→ Entity Framework Core
  │
  ├─── <<access>> ──→ SQL Server Database
  │
  └─── <<implements>> ──→ Repository Pattern
```

### B. Dependency Injection (DI) Configuration

```csharp
// Program.cs - Service Registration
services.AddScoped<IAccountService, AccountService>();
services.AddScoped<IProductService, ProductService>();
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<ICartService, CartService>();
services.AddScoped<IPaymentService, PaymentService>();
services.AddScoped<IBlogService, BlogService>();
services.AddScoped<IReviewService, ReviewService>();
services.AddScoped<IWalletService, WalletService>();
services.AddScoped<IPromotionService, PromotionService>();
services.AddScoped<IVoucherService, VoucherService>();
services.AddScoped<IChatService, ChatService>();
services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<IRefundService, RefundService>();
services.AddScoped<IStatisticsService, StatisticsService>();

// Repository DI
services.AddScoped<IGenericRepository<Account>, GenericRepository<Account>>();
services.AddScoped<IAccountRepository, AccountRepository>();
services.AddScoped<IProductRepository, ProductRepository>();
// ... more repositories
```

---

## IV. Fully Qualified Names (FQN)

### Package Naming Convention

```
ASP_LorKingDom.System.WebUI
├── ASP_LorKingDom.System.WebUI.Controllers
├── ASP_LorKingDom.System.WebUI.Views
├── ASP_LorKingDom.System.WebUI.ChatHubs
├── ASP_LorKingDom.System.WebUI.Filters
└── ASP_LorKingDom.System.WebUI.Workers

ASP_LorKingDom.System.BLL
├── ASP_LorKingDom.System.BLL.Services
├── ASP_LorKingDom.System.BLL.Interfaces
├── ASP_LorKingDom.System.BLL.DTOs
└── ASP_LorKingDom.System.BLL.Validators

ASP_LorKingDom.System.DAL
├── ASP_LorKingDom.System.DAL.Repositories
├── ASP_LorKingDom.System.DAL.Interfaces
├── ASP_LorKingDom.System.DAL.Models
└── ASP_LorKingDom.System.DAL.DbContext

ASP_LorKingDom.System.External
├── ASP_LorKingDom.System.External.PaymentGateway
├── ASP_LorKingDom.System.External.EmailService
└── ASP_LorKingDom.System.External.FileStorage
```

---

## V. Package Constraints & Rules

### 1. Unique Package Names ✓
- Mỗi package có tên duy nhất trong context của parent package
- Không có xung đột tên giữa các packages

### 2. Classes with Same Names (Allowed) ✓
```csharp
// Không xung đột vì nằm trong package khác nhau
WebUI.Models.Product        // Local DTO-like model
DAL.Models.Product          // Entity model
BLL.DTOs.ProductDto         // Data transfer object
```

### 3. Package Content Variability ✓
- `WebUI.Controllers`: Chứa các controller classes
- `DAL.Models`: Chứa entity models
- `BLL.Services`: Chứa service implementations
- `External`: Chứa integration configurations

### 4. No Circular Dependencies ✓
```
✓ VALID:   WebUI → BLL → DAL → Database
✗ INVALID: WebUI → BLL → WebUI (circular)
✗ INVALID: DAL → BLL → DAL (circular)
```

### 5. Visibility & Access Levels ✓
```csharp
// Public - accessible from all packages
public interface IAccountService { }
public class AccountService { }

// Internal - restricted to same assembly
internal class PaymentGatewayHelper { }

// Private - restricted to class/struct
private void ProcessPayment() { }
```

---

## VI. Workflow: Data Flow Through Packages

### Example: Product Purchase Flow

```
1. User Action (WebUI.Controllers.CartController)
   └─→ AddToCart(productId, quantity)
   
2. Business Logic Processing (BLL.Services.CartService)
   ├─→ Validate input using CartValidator
   ├─→ Retrieve product via IProductRepository
   ├─→ Check inventory
   └─→ Create CartDto

3. Data Persistence (DAL.Repositories.CartRepository)
   ├─→ Query existing cart
   ├─→ Add cart item to Cart entity
   └─→ SaveChanges() through DbContext

4. Response to Client (WebUI.Views)
   └─→ Return updated cart view with CartDto
```

### Example: Payment Processing Flow

```
1. Initiate Payment (WebUI.Controllers.OrderController)
   └─→ ProcessPayment(orderId, paymentMethod)

2. Business Logic (BLL.Services.PaymentService)
   ├─→ Validate order via IOrderRepository
   ├─→ Create payment request
   └─→ Call External Payment Gateway

3. External Integration
   ├─→ VNPay API (if VNPay method)
   ├─→ Momo API (if Momo method)
   └─→ Return payment URL to client

4. Verification (Webhook/Callback)
   ├─→ PaymentService verifies payment status
   ├─→ Update order status in DAL
   └─→ Send notification to user

5. Update Database (DAL)
   ├─→ OrderRepository updates Order entity
   ├─→ WalletRepository records transaction
   └─→ NotificationRepository logs notification
```

---

## VII. Technology Stack per Package

| Package | Technologies | Version |
|---------|-------------|---------|
| **WebUI** | ASP.NET Core MVC, SignalR, Razor Views, Bootstrap 5, jQuery | 6.0+ |
| **BLL** | C#, FluentValidation, AutoMapper, LINQ | 10.0+ |
| **DAL** | Entity Framework Core, SQL Server, LINQ | 6.0+ |
| **External** | VNPay SDK, Momo SDK, SMTP .NET | Latest |

---

## VIII. Stereotype Legend

```
<<import>>           - Strong dependency, importing all public elements
<<access>>           - Weak dependency, accessing specific elements
<<signal-r>>         - Real-time communication protocol
<<background-service>> - Background/scheduled service
<<fluent-validation>> - Using FluentValidation framework
<<entity-framework>> - Using Entity Framework Core
<<payment-gateway>>  - External payment service integration
<<persistent-storage>> - Database persistence layer
<<authorization-filter>> - Security/authorization filter
```

---

## IX. Comparison Chart

### Dependency Types Comparison

| Dependency Type | Usage | Direction | Visibility |
|---|---|---|---|
| **<<import>>** | Package A imports all public elements of Package B | A → B | All public |
| **<<access>>** | Package A accesses specific elements of Package B | A → B | Selected public |
| **None** | No direct dependency | - | No access |

---

## X. Design Benefits

### 1. **Separation of Concerns** ✓
- Each layer handles specific responsibility
- Easy to understand and maintain

### 2. **Testability** ✓
- Can test each layer independently
- Mock dependencies easily

### 3. **Reusability** ✓
- Services can be reused across controllers
- DTOs prevent tight coupling

### 4. **Scalability** ✓
- Can scale layers independently
- Easy to add new features

### 5. **Maintainability** ✓
- Clear structure makes code easier to navigate
- Team members understand architecture quickly

### 6. **Flexibility** ✓
- Easy to swap implementations (e.g., different payment gateways)
- Business logic isolated from presentation

---

## XI. Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=LorKingdom;..."
  },
  "VNPay": {
    "TmnCode": "***",
    "HashSecret": "***"
  },
  "Momo": {
    "PartnerCode": "***",
    "AccessKey": "***"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587
  }
}
```

---

## XII. Summary - Package Diagram Components

| Component | Count | Purpose |
|-----------|-------|---------|
| Main Packages | 3 | WebUI, BLL, DAL |
| Sub-packages | 12+ | Controllers, Services, Repositories, etc. |
| Dependencies | Multiple | <<import>>, <<access>> |
| External Services | 3 | VNPay, Momo, Email, Storage |
| Total Classes | 100+ | Controllers, Services, Repositories, Models |
| Database Tables | 20+ | Entity mappings |

---

## XIII. Future Extensibility Points

1. **Add Microservices**: Can split DAL into separate service for independent scaling
2. **Add Caching Layer**: Add caching package between BLL and DAL
3. **Add API Gateway**: Add API Gateway package above WebUI
4. **Message Queue**: Add event-driven architecture with message queue
5. **Logging & Monitoring**: Add cross-cutting concerns package

---

**Version:** 1.0  
**Created:** November 5, 2025  
**Architecture:** 3-Tier Layered Architecture  
**Framework:** ASP.NET Core MVC 6.0+  
**Database:** SQL Server
