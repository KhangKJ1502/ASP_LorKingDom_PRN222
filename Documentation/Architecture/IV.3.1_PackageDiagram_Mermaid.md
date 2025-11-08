# IV.3.1. Package Diagram - ASP_LorKingDom (Mermaid)

## Package Diagram - ASP_LorKingDom E-Commerce System

```mermaid
graph TB
    subgraph ASP_LorKingDom["ASP_LorKingDom System - E-Commerce Platform 3-Tier Architecture"]
        
        subgraph WebUI["📱 WebUI - Presentation Layer"]
            subgraph WebUIControllers["WebUI.Controllers"]
                AC["AccountCustomerController"]
                AS["AccountStaffController"]
                AU["AdminAuthController"]
                AD["AdminController"]
                AT["AuthController"]
                PC["ProductController"]
                CC["CategoryController"]
                CT["CartController"]
                OC["OrderController"]
                ORA["OrderRefundAdminController"]
                BC["BlogController"]
                BCC["BlogCategoryController"]
                BRC["BlogReviewController"]
                RC["ReviewController"]
                RPC["ReviewProductController"]
                WC["WalletController"]
                CHC["ChatController"]
                CDAC["ChatDashboardApiController"]
                NC["NotificationController"]
                MNC["MyNotificationsController"]
                VC["VoucherController"]
                PC2["PromotionController"]
                WLC["WishlistController"]
                BRC2["BrandController"]
                MC["MaterialController"]
                ORC["OriginController"]
                PRC["PriceRangeController"]
                SCC["SuperCategoryController"]
                ADC["AddressController"]
                SC["StatisticsController"]
                HC["HomeController"]
            end
            
            subgraph WebUIViews["WebUI.Views - Razor Templates"]
                VA["Admin/"]
                VAA["AdminAuth/"]
                VAT["Auth/"]
                VB["Blog/"]
                VCH["Chat/"]
                VH["Home/"]
                VMN["MyNotifications/"]
                VR["Review/"]
                VS["Shared/"]
                VW["Wallet/"]
                VWL["Wishlist/"]
            end
            
            subgraph WebUIChatHubs["WebUI.ChatHubs - SignalR"]
                CH["ChatHub"]
                CHM["SendMessage()"]
                CHJ["JoinGroup()"]
                CHL["LeaveGroup()"]
                CHR["ReceiveMessage()"]
            end
            
            subgraph WebUIFilters["WebUI.Filters"]
                AAA["AdminAuthorizationAttributes"]
            end
            
            subgraph WebUIWorkers["WebUI.Workers - Background Services"]
                NWS["NotificationWorkerService"]
                PWS["PromotionWorkerService"]
            end
            
            subgraph WebUIAssets["wwwroot - Static Assets"]
                CSS["css/"]
                JS["js/"]
                LIB["lib/"]
                UP["uploads/"]
                AS2["Assets/"]
            end
        end
        
        WebUI -->|"<<import>>"| BLLServices
        WebUI -->|"<<access>>"| BLLDTOs
        
        subgraph BLL["🔧 BLL - Business Logic Layer"]
            subgraph BLLServices["BLL.Services"]
                ACS["AccountService"]
                PRS["ProductService"]
                ORS["OrderService"]
                CRS["CartService"]
                PYS["PaymentService"]
                BS["BlogService"]
                RVS["ReviewService"]
                WS["WalletService"]
                PROM["PromotionService"]
                VOU["VoucherService"]
                CHS["ChatService"]
                NS["NotificationService"]
                RFS["RefundService"]
                STATS["StatisticsService"]
                CES["CategoryService"]
                BES["BrandService"]
                MAT["MaterialService"]
                ORI["OriginService"]
                ADS["AddressService"]
                WLS["WishlistService"]
                SCAT["SuperCategoryService"]
            end
            
            subgraph BLLInterfaces["BLL.Interfaces - Service Contracts"]
                IAS["IAccountService"]
                IPS["IProductService"]
                IOS["IOrderService"]
                ICS["ICartService"]
                IPYS["IPaymentService"]
                IBS["IBlogService"]
                IRVS["IReviewService"]
                IWS["IWalletService"]
                IPROM["IPromotionService"]
                IVOU["IVoucherService"]
                ICHS["IChatService"]
                INS["INotificationService"]
                IRFS["IRefundService"]
                ISTATS["IStatisticsService"]
                ICES["ICategoryService"]
                IBES["IBrandService"]
                IMAT["IMaterialService"]
                IORI["IOriginService"]
                IADS["IAddressService"]
                IWLS["IWishlistService"]
                ISCAT["ISuperCategoryService"]
            end
            
            subgraph BLLDTOs["BLL.DTOs - Data Transfer Objects"]
                ADTO["AccountDto"]
                PDTO["ProductDto"]
                ODTO["OrderDto"]
                CDTO["CartDto"]
                BDTO["BlogDto"]
                RDTO["ReviewDto"]
                WDTO["WalletDto"]
                CHDTO["ChatDto"]
                NDTO["NotificationDto"]
                VDTO["VoucherDto"]
                PROMDTO["PromotionDto"]
                REFDTO["RefundDto"]
                CATDTO["CategoryDto"]
                BEDTO["BrandDto"]
                MATDTO["MaterialDto"]
                ORIDTO["OriginDto"]
                ADDTO["AddressDto"]
                WLDTO["WishlistDto"]
                STATSDTO["StatisticsDto"]
            end
            
            subgraph BLLValidators["BLL.Validators - FluentValidation"]
                AVAL["AccountValidator"]
                PVAL["ProductValidator"]
                OVAL["OrderValidator"]
                CVAL["CartValidator"]
                BVAL["BlogValidator"]
                RVAL["ReviewValidator"]
                PYVAL["PaymentValidator"]
            end
        end
        
        BLL -->|"<<import>>"| DALRepositories
        BLL -->|"<<access>>"| DALModels
        BLL -->|"<<access>>"| ExternalServices
        
        subgraph DAL["💾 DAL - Data Access Layer"]
            subgraph DALRepositories["DAL.Repositories"]
                GREP["GenericRepository<T>"]
                AREP["AccountRepository"]
                PREP["ProductRepository"]
                OREP["OrderRepository"]
                ODREP["OrderDetailRepository"]
                CREP["CartRepository"]
                CIREP["CartItemRepository"]
                CATREP["CategoryRepository"]
                SCREP["SuperCategoryRepository"]
                BREP["BrandRepository"]
                MREP["MaterialRepository"]
                OIREP["OriginRepository"]
                BRLOG["BlogRepository"]
                BCATREP["BlogCategoryRepository"]
                RREP["ReviewRepository"]
                WREP["WalletRepository"]
                TREP["TransactionRepository"]
                PROMREP["PromotionRepository"]
                VOUREP["VoucherRepository"]
                CHREP["ChatRepository"]
                MSGREP["MessageRepository"]
                NREP["NotificationRepository"]
                REFREP["RefundRequestRepository"]
                WLREP["WishlistRepository"]
                ADREP["AddressRepository"]
            end
            
            subgraph DALInterfaces["DAL.Interfaces - Repository Contracts"]
                IREP["IGenericRepository<T>"]
                IARES["IAccountRepository"]
                IPRES["IProductRepository"]
                IORES["IOrderRepository"]
                ICRES["ICartRepository"]
                ICATES["ICategoryRepository"]
                IBRES["IBlogRepository"]
                IRRES["IReviewRepository"]
                IWRES["IWalletRepository"]
                IPROMRES["IPromotionRepository"]
                IVOURES["IVoucherRepository"]
                ICHRES["IChatRepository"]
                INRES["INotificationRepository"]
                IREFRES["IRefundRequestRepository"]
                IBRES2["IBrandRepository"]
                IMRES["IMaterialRepository"]
                IORES2["IOriginRepository"]
                IWLRES["IWishlistRepository"]
                IADRES["IAddressRepository"]
                ISCRES["ISuperCategoryRepository"]
            end
            
            subgraph DALModels["DAL.Models - Entity Models"]
                ACC["Account"]
                PRD["Product"]
                ORD["Order"]
                ORDT["OrderDetail"]
                CRT["Cart"]
                CRTT["CartItem"]
                CAT["Category"]
                SCAT2["SuperCategory"]
                BRD["Brand"]
                MAL["Material"]
                ORI2["Origin"]
                BLOG["Blog"]
                BCAT["BlogCategory"]
                REV["Review"]
                WAL["Wallet"]
                TRAN["Transaction"]
                PROM["Promotion"]
                VOU["Voucher"]
                CHT["Chat"]
                MSG["Message"]
                NOT["Notification"]
                REF["RefundRequest"]
                WISH["Wishlist"]
                ADDR["Address"]
            end
            
            subgraph DALDbContext["DAL.DbContext"]
                DBCTX["LorKingdomDbContext"]
                EFC["Entity Framework Core DbContext"]
                DBCFG["Database Configuration"]
                RELCFG["Relationship Configurations"]
                DBSEED["Data Seeding"]
                DBMIG["Migration Support"]
            end
            
            subgraph Database["🗄️ SQL Server Database"]
                TABLES["Accounts, Products, Orders, Carts, Categories,<br/>Blogs, Reviews, Wallets, Transactions, Promotions,<br/>Vouchers, Chats, Messages, Notifications,<br/>RefundRequests, Wishlists, Addresses, Brands,<br/>Materials, Origins"]
            end
        end
        
        DAL --> Database
        
        subgraph ExternalServices["🌐 External Services & Integrations"]
            subgraph PaymentGateway["Payment Gateway Services"]
                VNP["VNPay Payment Gateway"]
                VNPCU["Create Payment URL"]
                VNPVP["Verify Payment"]
                VNPQS["Query Transaction Status"]
                
                MMO["Momo Payment Gateway"]
                MMOCU["Create Payment URL"]
                MMOVP["Verify Payment"]
                MMOQS["Query Transaction Status"]
            end
            
            subgraph EmailService["Email Service"]
                SMTP["SMTP Configuration"]
                SEN["Send Email Notifications"]
                ORC2["Order Confirmation"]
                PSW["Password Reset"]
                PROMO["Promotion Updates"]
            end
            
            subgraph FileStorage["File Storage Service"]
                UPIMG["Upload Product Images"]
                UPAVT["Upload User Avatars"]
                UPBLOG["Upload Blog Images"]
                UPWS["Local wwwroot/uploads/"]
            end
        end
        
        BLL -->|"<<access>>"| ExternalServices
    end
    
    style WebUI fill:#e1f5ff
    style BLL fill:#fff3e0
    style DAL fill:#f3e5f5
    style ExternalServices fill:#e8f5e9
    style Database fill:#fce4ec
