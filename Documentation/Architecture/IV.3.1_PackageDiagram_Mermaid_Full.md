# Package Diagram - ASP_LorKingDom (Mermaid - Simplified View)

## Simplified Package Diagram - Main Layers & Dependencies

```mermaid
graph TD
    subgraph System["🎯 ASP_LorKingDom System - 3-Tier Architecture"]
        subgraph Presentation["📱 Presentation Layer - WebUI"]
            WebUIControllers["WebUI.Controllers<br/>(25+ Controllers)"]
            WebUIViews["WebUI.Views<br/>(Razor Templates)"]
            WebUIHub["WebUI.ChatHubs<br/>(SignalR)"]
            WebUIFilters["WebUI.Filters<br/>(Authorization)"]
            WebUIWorkers["WebUI.Workers<br/>(Background Tasks)"]
            WebUIAssets["wwwroot<br/>(Static Assets)"]
        end
        
        subgraph Business["🔧 Business Logic Layer - BLL"]
            BLLServices["BLL.Services<br/>(18+ Services)"]
            BLLInterfaces["BLL.Interfaces<br/>(Service Contracts)"]
            BLLDTOs["BLL.DTOs<br/>(19+ Data Objects)"]
            BLLValidators["BLL.Validators<br/>(FluentValidation)"]
        end
        
        subgraph Data["💾 Data Access Layer - DAL"]
            DALRepositories["DAL.Repositories<br/>(25+ Repositories)"]
            DALInterfaces["DAL.Interfaces<br/>(Repository Contracts)"]
            DALModels["DAL.Models<br/>(24+ Entities)"]
            DALDbContext["DAL.DbContext<br/>(LorKingdomDbContext)"]
        end
        
        subgraph External["🌐 External Services"]
            PaymentGW["Payment Gateway<br/>(VNPay, Momo)"]
            EmailService["Email Service<br/>(SMTP)"]
            FileStorage["File Storage<br/>(wwwroot/uploads)"]
        end
        
        subgraph PersistentLayer["🗄️ Persistent Storage"]
            Database[(SQL Server<br/>Database)]
        end
    end
    
    %% Dependencies
    WebUIControllers -->|"<<import>>"| BLLServices
    WebUIControllers -->|"<<access>>"| BLLDTOs
    WebUIViews -->|"<<access>>"| BLLDTOs
    WebUIHub -->|"<<import>>"| BLLServices
    WebUIWorkers -->|"<<import>>"| BLLServices
    
    BLLServices -->|"<<import>>"| BLLInterfaces
    BLLServices -->|"<<import>>"| DALRepositories
    BLLServices -->|"<<access>>"| DALModels
    BLLServices -->|"<<access>>"| BLLDTOs
    BLLServices -->|"<<access>>"| BLLValidators
    BLLServices -->|"<<access>>"| External
    
    DALRepositories -->|"<<import>>"| DALInterfaces
    DALRepositories -->|"<<access>>"| DALModels
    DALRepositories -->|"<<import>>"| DALDbContext
    
    DALDbContext -->|"<<access>>"| Database
    
    BLLServices -->|"<<access>>"| PaymentGW
    BLLServices -->|"<<access>>"| EmailService
    BLLServices -->|"<<access>>"| FileStorage
    
    %% Styling
    classDef webui fill:#e1f5ff,stroke:#01579b,stroke-width:2px,color:#000
    classDef bll fill:#fff3e0,stroke:#e65100,stroke-width:2px,color:#000
    classDef dal fill:#f3e5f5,stroke:#4a148c,stroke-width:2px,color:#000
    classDef external fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px,color:#000
    classDef database fill:#fce4ec,stroke:#880e4f,stroke-width:2px,color:#000
    
    class WebUIControllers,WebUIViews,WebUIHub,WebUIFilters,WebUIWorkers,WebUIAssets webui
    class BLLServices,BLLInterfaces,BLLDTOs,BLLValidators bll
    class DALRepositories,DALInterfaces,DALModels,DALDbContext dal
    class PaymentGW,EmailService,FileStorage external
    class Database database
```

---

## Detailed Package Diagram - All Components

```mermaid
graph TB
    subgraph WebUI["WebUI - Presentation Layer"]
        subgraph Controllers["Controllers (25+)"]
            C1["AccountCustomerController<br/>AccountStaffController<br/>AdminAuthController"]
            C2["ProductController<br/>CategoryController<br/>BrandController"]
            C3["CartController<br/>OrderController<br/>OrderRefundAdminController"]
            C4["PaymentController<br/>WalletController<br/>VoucherController"]
            C5["BlogController<br/>BlogCategoryController<br/>ReviewController"]
            C6["ChatController<br/>NotificationController<br/>StatisticsController"]
        end
        
        subgraph Views["Views & Assets"]
            V1["Razor Templates:<br/>Admin, Auth, Blog, Chat<br/>Home, Wallet, Review"]
            V2["Static Assets:<br/>CSS, JS, Images<br/>Uploads"]
        end
        
        subgraph RealTime["Real-Time & Background"]
            RT["ChatHub SignalR<br/>NotificationWorkerService<br/>PromotionWorkerService"]
        end
        
        subgraph Security["Security"]
            SEC["AdminAuthorizationAttributes<br/>Role-based Access Control"]
        end
    end
    
    subgraph BLL["BLL - Business Logic Layer"]
        subgraph Services["Services (18+)"]
            S1["AccountService<br/>ProductService<br/>OrderService"]
            S2["CartService<br/>PaymentService<br/>BlogService"]
            S3["ReviewService<br/>WalletService<br/>PromotionService"]
            S4["VoucherService<br/>ChatService<br/>NotificationService"]
            S5["RefundService<br/>StatisticsService<br/>CategoryService"]
        end
        
        subgraph Interfaces["Interfaces (21)"]
            I["IAccountService<br/>IProductService<br/>IOrderService<br/>IPaymentService<br/>...and more"]
        end
        
        subgraph DTOs["DTOs (19+)"]
            D["AccountDto<br/>ProductDto<br/>OrderDto<br/>CartDto<br/>PaymentDto<br/>...and more"]
        end
        
        subgraph Validators["Validators"]
            VAL["FluentValidation<br/>AccountValidator<br/>ProductValidator<br/>OrderValidator"]
        end
    end
    
    subgraph DAL["DAL - Data Access Layer"]
        subgraph Repos["Repositories (25+)"]
            R1["GenericRepository<T><br/>AccountRepository<br/>ProductRepository"]
            R2["OrderRepository<br/>CartRepository<br/>CategoryRepository"]
            R3["BlogRepository<br/>ReviewRepository<br/>WalletRepository"]
            R4["PromotionRepository<br/>VoucherRepository<br/>ChatRepository"]
            R5["NotificationRepository<br/>RefundRequestRepository<br/>AddressRepository"]
        end
        
        subgraph RepoInterfaces["Repository Interfaces (20+)"]
            RI["IGenericRepository<T><br/>IAccountRepository<br/>IProductRepository<br/>IOrderRepository<br/>...and more"]
        end
        
        subgraph Entities["Entity Models (24+)"]
            E1["Account, Product, Order<br/>OrderDetail, Cart, CartItem"]
            E2["Category, SuperCategory<br/>Brand, Material, Origin"]
            E3["Blog, BlogCategory<br/>Review, Wallet, Transaction"]
            E4["Promotion, Voucher<br/>Chat, Message, Notification"]
            E5["RefundRequest, Wishlist<br/>Address"]
        end
        
        subgraph DbCtx["DbContext"]
            DB["LorKingdomDbContext<br/>Entity Framework Core<br/>Database Configuration<br/>Migrations"]
        end
    end
    
    subgraph Database["SQL Server Database"]
        Tables["Accounts, Products, Orders, Carts<br/>Categories, Blogs, Reviews, Wallets<br/>Promotions, Vouchers, Chats, Messages<br/>Notifications, RefundRequests, Wishlists<br/>Addresses, Brands, Materials, Origins"]
    end
    
    subgraph External["External Services"]
        Payment["VNPay Gateway<br/>Momo Gateway<br/>Payment Processing"]
        Email["SMTP Email Service<br/>Notifications<br/>Confirmations"]
        Files["File Storage<br/>Image Uploads<br/>wwwroot/uploads"]
    end
    
    %% Dependencies with arrows
    C1 -->|import| S1
    C2 -->|import| S2
    C3 -->|import| S3
    C4 -->|import| S4
    C5 -->|import| S2
    C6 -->|import| S4
    
    RT -->|import| S4
    
    V1 -->|access| D
    
    S1 -->|import| R1
    S2 -->|import| R2
    S3 -->|import| R3
    S4 -->|import| R4
    S5 -->|import| R5
    
    S1 -->|access| I
    S2 -->|access| I
    S3 -->|access| I
    S4 -->|access| I
    S5 -->|access| I
    
    S1 -->|access| VAL
    
    S1 -->|access| Payment
    S3 -->|access| Email
    S5 -->|access| Files
    
    R1 -->|import| RI
    R2 -->|import| RI
    R3 -->|import| RI
    R4 -->|import| RI
    R5 -->|import| RI
    
    R1 -->|access| E1
    R2 -->|access| E2
    R3 -->|access| E3
    R4 -->|access| E4
    R5 -->|access| E5
    
    R1 -->|access| DB
    R2 -->|access| DB
    R3 -->|access| DB
    R4 -->|access| DB
    R5 -->|access| DB
    
    DB -->|persist| Tables
    
    %% Styling
    classDef webui fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef bll fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef dal fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef external fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    classDef database fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    
    class WebUI,Controllers,Views,RealTime,Security webui
    class BLL,Services,Interfaces,DTOs,Validators bll
    class DAL,Repos,RepoInterfaces,Entities,DbCtx dal
    class External,Payment,Email,Files external
    class Database,Tables database
```

---

## Dependency Flow Diagram

```mermaid
graph LR
    A["🖥️ WebUI Layer<br/>Controllers + Views"] 
    B["🔧 BLL Layer<br/>Business Logic"]
    C["💾 DAL Layer<br/>Data Access"]
    D["🗄️ Database<br/>SQL Server"]
    E["🌐 External APIs"]
    
    A -->|"<<import>><br/>Services"| B
    A -->|"<<access>><br/>DTOs"| B
    
    B -->|"<<import>><br/>Repositories"| C
    B -->|"<<access>><br/>Entities"| C
    B -->|"<<access>><br/>Payment,Email"| E
    
    C -->|"<<read/write>><br/>SQL"| D
    
    style A fill:#e1f5ff,stroke:#01579b,stroke-width:3px
    style B fill:#fff3e0,stroke:#e65100,stroke-width:3px
    style C fill:#f3e5f5,stroke:#4a148c,stroke-width:3px
    style D fill:#fce4ec,stroke:#880e4f,stroke-width:3px
    style E fill:#e8f5e9,stroke:#1b5e20,stroke-width:3px
```

---

## Component Count Summary

```mermaid
graph TB
    subgraph stats["📊 Component Statistics"]
        subgraph webui_stats["WebUI Layer"]
            wc["25 Controllers"]
            wv["10+ Views"]
            wh["1 ChatHub SignalR"]
            wf["1 Filter Attribute"]
            ww["2 Background Workers"]
        end
        
        subgraph bll_stats["BLL Layer"]
            bs["18 Services"]
            bi["21 Service Interfaces"]
            bd["19 DTOs"]
            bv["7 Validators"]
        end
        
        subgraph dal_stats["DAL Layer"]
            dr["25 Repositories"]
            dri["20 Repository Interfaces"]
            de["24 Entity Models"]
            ddb["1 DbContext"]
        end
        
        subgraph ext_stats["External"]
            ep["2 Payment Gateways"]
            ee["1 Email Service"]
            ef["1 File Storage"]
        end
    end
    
    style webui_stats fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    style bll_stats fill:#fff3e0,stroke:#e65100,stroke-width:2px
    style dal_stats fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    style ext_stats fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
```

---

## Data Flow Example: Place Order

```mermaid
sequenceDiagram
    actor User
    participant WUI as WebUI<br/>OrderController
    participant BLL as BLL.Services<br/>OrderService
    participant DAL as DAL.Repositories
    participant DB as Database
    
    User->>WUI: PlaceOrder(cartId)
    activate WUI
    
    WUI->>BLL: PlaceOrderAsync(cartDto)
    activate BLL
    
    BLL->>BLL: ValidateCart()
    BLL->>DAL: GetCartItemsAsync()
    activate DAL
    
    DAL->>DB: Query CartItems
    DB-->>DAL: Return CartItems
    
    DAL-->>BLL: CartItems
    deactivate DAL
    
    BLL->>BLL: CalculateTotal()
    BLL->>DAL: CreateOrderAsync(order)
    activate DAL
    
    DAL->>DB: INSERT Order
    DAL->>DB: INSERT OrderDetails
    DB-->>DAL: Success
    
    DAL-->>BLL: OrderId
    deactivate DAL
    
    BLL->>BLL: CreateNotification()
    BLL-->>WUI: OrderDto
    deactivate BLL
    
    WUI-->>User: Show Confirmation
    deactivate WUI
```

---

## Data Flow Example: Process Payment

```mermaid
sequenceDiagram
    actor User
    participant WUI as WebUI<br/>OrderController
    participant BLL as BLL.Services<br/>PaymentService
    participant EXT as External<br/>VNPay API
    participant DAL as DAL.Repositories
    participant DB as Database
    
    User->>WUI: ProcessPayment(orderId, method)
    activate WUI
    
    WUI->>BLL: InitiatePaymentAsync(orderId)
    activate BLL
    
    BLL->>DAL: GetOrderAsync(orderId)
    activate DAL
    DAL->>DB: Query Order
    DB-->>DAL: Order Details
    DAL-->>BLL: Order
    deactivate DAL
    
    alt if VNPay
        BLL->>EXT: CreatePaymentUrl(amount)
        activate EXT
        EXT-->>BLL: PaymentUrl
        deactivate EXT
    end
    
    BLL-->>WUI: PaymentUrl
    deactivate BLL
    
    WUI-->>User: Redirect to VNPay
    
    User->>EXT: Complete Payment
    activate EXT
    EXT->>BLL: Webhook Callback
    activate BLL
    
    BLL->>DAL: UpdateOrderStatus(paid)
    activate DAL
    DAL->>DB: UPDATE Order Status
    DAL->>DB: INSERT Transaction
    DB-->>DAL: Success
    deactivate DAL
    
    BLL->>DAL: SendNotification()
    BLL-->>EXT: Acknowledge
    deactivate BLL
    deactivate EXT
    
    BLL-->>WUI: Payment Confirmed
    WUI-->>User: Show Success Message
    deactivate WUI
```

---

## Dependency Injection Setup

```mermaid
graph TB
    subgraph DI["Dependency Injection Configuration - Program.cs"]
        subgraph Services["Service Registration"]
            S1["services.AddScoped IAccountService<br/>services.AddScoped IProductService<br/>services.AddScoped IOrderService"]
            S2["services.AddScoped ICartService<br/>services.AddScoped IPaymentService<br/>services.AddScoped IBlogService"]
            S3["services.AddScoped IReviewService<br/>services.AddScoped IWalletService<br/>...more services"]
        end
        
        subgraph Repos["Repository Registration"]
            R1["services.AddScoped IGenericRepository<T><br/>services.AddScoped IAccountRepository<br/>services.AddScoped IProductRepository"]
            R2["services.AddScoped IOrderRepository<br/>services.AddScoped ICartRepository<br/>...more repositories"]
        end
        
        subgraph DbCfg["Database Configuration"]
            DB["services.AddDbContext LorKingdomDbContext<br/>services.AddMigrations"]
        end
        
        subgraph External["External Services"]
            EXT["Payment Gateway Config<br/>Email Service Config<br/>File Storage Config"]
        end
    end
    
    style Services fill:#fff3e0,stroke:#e65100,stroke-width:2px
    style Repos fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    style DbCfg fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    style External fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
```

---

## Architecture Benefits

```mermaid
graph TB
    subgraph Benefits["🎯 3-Tier Architecture Benefits"]
        B1["✅ Separation of Concerns<br/>Each layer has specific responsibility"]
        B2["✅ Testability<br/>Can test layers independently"]
        B3["✅ Reusability<br/>Services reused across controllers"]
        B4["✅ Maintainability<br/>Clear structure, easy navigation"]
        B5["✅ Scalability<br/>Scale layers independently"]
        B6["✅ Flexibility<br/>Easy to swap implementations"]
    end
    
    style B1 fill:#c8e6c9,stroke:#2e7d32,stroke-width:2px
    style B2 fill:#c8e6c9,stroke:#2e7d32,stroke-width:2px
    style B3 fill:#c8e6c9,stroke:#2e7d32,stroke-width:2px
    style B4 fill:#c8e6c9,stroke:#2e7d32,stroke-width:2px
    style B5 fill:#c8e6c9,stroke:#2e7d32,stroke-width:2px
    style B6 fill:#c8e6c9,stroke:#2e7d32,stroke-width:2px
```

---

**Version:** 1.0 - Mermaid Diagrams  
**Created:** November 5, 2025  
**Framework:** ASP.NET Core MVC 6.0+  
**Architecture:** 3-Tier Layered Architecture  
**Visualization:** Mermaid Flowcharts & Sequence Diagrams
