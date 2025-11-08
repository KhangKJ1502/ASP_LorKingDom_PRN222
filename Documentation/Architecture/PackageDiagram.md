# UML Package Diagram - ASP_LorKingDom E-Commerce System

## Overview
This package diagram illustrates the hierarchical structure and dependencies of the ASP_LorKingDom e-commerce application, organized using 3-tier architecture pattern.

## Package Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ASP_LorKingDom.System                               │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │                         WebUI (Presentation Layer)                     │ │
│  │  <<web application>>                                                  │ │
│  │                                                                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │ │
│  │  │ Controllers  │  │    Views     │  │   ChatHubs   │              │ │
│  │  │              │  │              │  │  <<signalr>> │              │ │
│  │  │ - Account    │  │ - Admin      │  │              │              │ │
│  │  │ - Product    │  │ - Auth       │  │ - ChatHub    │              │ │
│  │  │ - Order      │  │ - Blog       │  │              │              │ │
│  │  │ - Cart       │  │ - Chat       │  │              │              │ │
│  │  │ - Payment    │  │ - Home       │  │              │              │ │
│  │  │ - Blog       │  │ - Wallet     │  │              │              │ │
│  │  │ - Review     │  │ - Shared     │  │              │              │ │
│  │  │ - Wallet     │  │              │  │              │              │ │
│  │  │ - Chat       │  │              │  │              │              │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │ │
│  │                                                                       │ │
│  │  ┌──────────────┐  ┌──────────────┐                                 │ │
│  │  │   Filters    │  │   Workers    │                                 │ │
│  │  │              │  │<<background>>│                                 │ │
│  │  │ - AdminAuth  │  │              │                                 │ │
│  │  │   Attributes │  │ - Notification│                                 │ │
│  │  │              │  │   Worker     │                                 │ │
│  │  │              │  │ - Promotion  │                                 │ │
│  │  │              │  │   Worker     │                                 │ │
│  │  └──────────────┘  └──────────────┘                                 │ │
│  │                                                                       │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                   │                                         │
│                                   │ <<import>>                              │
│                                   │ <<access>>                              │
│                                   ▼                                         │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │                      BLL (Business Logic Layer)                       │ │
│  │  <<business logic>>                                                   │ │
│  │                                                                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │ │
│  │  │   Services   │  │     DTOs     │  │  Validators  │              │ │
│  │  │              │  │              │  │<<validation>>│              │ │
│  │  │ - Account    │  │ - AccountDto │  │              │              │ │
│  │  │ - Product    │  │ - ProductDto │  │ - Account    │              │ │
│  │  │ - Order      │  │ - OrderDto   │  │   Validator  │              │ │
│  │  │ - Cart       │  │ - CartDto    │  │ - Product    │              │ │
│  │  │ - Payment    │  │ - BlogDto    │  │   Validator  │              │ │
│  │  │ - Blog       │  │ - ReviewDto  │  │ - Order      │              │ │
│  │  │ - Review     │  │ - WalletDto  │  │   Validator  │              │ │
│  │  │ - Wallet     │  │ - ChatDto    │  │              │              │ │
│  │  │ - Promotion  │  │ - Voucher    │  │              │              │ │
│  │  │ - Voucher    │  │   Dto        │  │              │              │ │
│  │  │ - Chat       │  │ - Notification│ │              │              │ │
│  │  │ - Notification│ │   Dto        │  │              │              │ │
│  │  │ - Refund     │  │ - RefundDto  │  │              │              │ │
│  │  │ - Statistics │  │              │  │              │              │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │ │
│  │                                                                       │ │
│  │  ┌──────────────────────────────────┐                                │ │
│  │  │         Interfaces               │                                │ │
│  │  │  <<interface>>                   │                                │ │
│  │  │                                  │                                │ │
│  │  │  - IAccountService               │                                │ │
│  │  │  - IProductService               │                                │ │
│  │  │  - IOrderService                 │                                │ │
│  │  │  - ICartService                  │                                │ │
│  │  │  - IPaymentService               │                                │ │
│  │  │  - IBlogService                  │                                │ │
│  │  │  - IReviewService                │                                │ │
│  │  │  - IWalletService                │                                │ │
│  │  │  - IChatService                  │                                │ │
│  │  │  - INotificationService          │                                │ │
│  │  │  - IPromotionService             │                                │ │
│  │  │  - IVoucherService               │                                │ │
│  │  │  - IRefundService                │                                │ │
│  │  │  - IStatisticsService            │                                │ │
│  │  └──────────────────────────────────┘                                │ │
│  │                                                                       │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                   │                                         │
│                                   │ <<import>>                              │
│                                   │ <<access>>                              │
│                                   ▼                                         │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │                       DAL (Data Access Layer)                         │ │
│  │  <<data access>>                                                      │ │
│  │                                                                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │ │
│  │  │ Repositories │  │    Models    │  │   DbContext  │              │ │
│  │  │              │  │  <<entity>>  │  │              │              │ │
│  │  │ - Account    │  │              │  │ - LorKingdom │              │ │
│  │  │ - Product    │  │ - Account    │  │   DbContext  │              │ │
│  │  │ - Order      │  │ - Product    │  │              │              │ │
│  │  │ - OrderDetail│  │ - Order      │  │              │              │ │
│  │  │ - Cart       │  │ - OrderDetail│  │              │              │ │
│  │  │ - CartItem   │  │ - Cart       │  │              │              │ │
│  │  │ - Category   │  │ - CartItem   │  │              │              │ │
│  │  │ - SuperCat   │  │ - Category   │  │              │              │ │
│  │  │ - Brand      │  │ - SuperCat   │  │              │              │ │
│  │  │ - Material   │  │ - Brand      │  │              │              │ │
│  │  │ - Origin     │  │ - Material   │  │              │              │ │
│  │  │ - Blog       │  │ - Origin     │  │              │              │ │
│  │  │ - BlogCat    │  │ - Blog       │  │              │              │ │
│  │  │ - Review     │  │ - BlogCat    │  │              │              │ │
│  │  │ - Wallet     │  │ - Review     │  │              │              │ │
│  │  │ - Transaction│  │ - Wallet     │  │              │              │ │
│  │  │ - Promotion  │  │ - Transaction│  │              │              │ │
│  │  │ - Voucher    │  │ - Promotion  │  │              │              │ │
│  │  │ - Chat       │  │ - Voucher    │  │              │              │ │
│  │  │ - Message    │  │ - Chat       │  │              │              │ │
│  │  │ - Notification│ │ - Message    │  │              │              │ │
│  │  │ - RefundReq  │  │ - Notification│ │              │              │ │
│  │  │ - Wishlist   │  │ - RefundReq  │  │              │              │ │
│  │  │ - Address    │  │ - Wishlist   │  │              │              │ │
│  │  │              │  │ - Address    │  │              │              │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │ │
│  │                                                                       │ │
│  │  ┌──────────────────────────────────┐                                │ │
│  │  │         Interfaces               │                                │ │
│  │  │  <<interface>>                   │                                │ │
│  │  │                                  │                                │ │
│  │  │  - IAccountRepository            │                                │ │
│  │  │  - IProductRepository            │                                │ │
│  │  │  - IOrderRepository              │                                │ │
│  │  │  - ICartRepository               │                                │ │
│  │  │  - ICategoryRepository           │                                │ │
│  │  │  - IBlogRepository               │                                │ │
│  │  │  - IReviewRepository             │                                │ │
│  │  │  - IWalletRepository             │                                │ │
│  │  │  - IPromotionRepository          │                                │ │
│  │  │  - IVoucherRepository            │                                │ │
│  │  │  - IChatRepository               │                                │ │
│  │  │  - INotificationRepository       │                                │ │
│  │  │  - IRefundRequestRepository      │                                │ │
│  │  │  - IGenericRepository<T>         │                                │ │
│  │  └──────────────────────────────────┘                                │ │
│  │                                                                       │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │                      External Services                                │ │
│  │  <<external>>                                                         │ │
│  │                                                                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │ │
│  │  │   Payment    │  │    Email     │  │   Storage    │              │ │
│  │  │   Gateway    │  │   Service    │  │   Service    │              │ │
│  │  │              │  │              │  │              │              │ │
│  │  │ - VNPay API  │  │ - SMTP       │  │ - File       │              │ │
│  │  │ - Momo API   │  │              │  │   Upload     │              │ │
│  │  │              │  │              │  │              │              │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │ │
│  │           ▲                ▲                ▲                         │ │
│  │           └────────────────┴────────────────┘                         │ │
│  │                          <<access>>                                   │ │
│  │                    (from BLL.Services)                                │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Package Descriptions

### 1. WebUI (Presentation Layer)
**Fully Qualified Name:** `ASP_LorKingDom.System.WebUI`

**Purpose:** Handles user interface, HTTP requests/responses, and real-time communication.

**Sub-packages:**
- `WebUI.Controllers`: Contains MVC controllers for handling HTTP requests
- `WebUI.Views`: Razor views for rendering UI
- `WebUI.ChatHubs`: SignalR hubs for real-time chat functionality
- `WebUI.Filters`: Custom authorization and action filters
- `WebUI.Workers`: Background services for notifications and promotions

**Dependencies:**
- <<import>> BLL.Services
- <<access>> BLL.DTOs
- <<access>> BLL.Interfaces

---

### 2. BLL (Business Logic Layer)
**Fully Qualified Name:** `ASP_LorKingDom.System.BLL`

**Purpose:** Implements business rules, validations, and orchestrates data flow.

**Sub-packages:**
- `BLL.Services`: Business logic implementations
  - AccountService
  - ProductService
  - OrderService
  - CartService
  - PaymentService
  - BlogService
  - ReviewService
  - WalletService
  - PromotionService
  - VoucherService
  - ChatService
  - NotificationService
  - RefundService
  - StatisticsService

- `BLL.DTOs`: Data Transfer Objects for inter-layer communication
  - AccountDto
  - ProductDto
  - OrderDto
  - CartDto
  - BlogDto
  - ReviewDto
  - WalletDto
  - ChatDto
  - VoucherDto
  - NotificationDto
  - RefundDto

- `BLL.Validators`: FluentValidation validators for input validation
  - AccountValidator
  - ProductValidator
  - OrderValidator

- `BLL.Interfaces`: Service contracts
  - IAccountService
  - IProductService
  - IOrderService
  - ... (other service interfaces)

**Dependencies:**
- <<import>> DAL.Repositories
- <<import>> DAL.Models
- <<access>> DAL.Interfaces
- <<access>> External.PaymentGateway
- <<access>> External.EmailService

---

### 3. DAL (Data Access Layer)
**Fully Qualified Name:** `ASP_LorKingDom.System.DAL`

**Purpose:** Manages database operations and data persistence.

**Sub-packages:**
- `DAL.Repositories`: Repository implementations using Repository Pattern
  - AccountRepository
  - ProductRepository
  - OrderRepository
  - CartRepository
  - CategoryRepository
  - BlogRepository
  - ReviewRepository
  - WalletRepository
  - PromotionRepository
  - VoucherRepository
  - ChatRepository
  - NotificationRepository
  - RefundRequestRepository
  - GenericRepository<T>

- `DAL.Models`: Entity models mapped to database tables
  - Account
  - Product
  - Order
  - OrderDetail
  - Cart
  - CartItem
  - Category
  - SuperCategory
  - Brand
  - Material
  - Origin
  - Blog
  - BlogCategory
  - Review
  - Wallet
  - Transaction
  - Promotion
  - Voucher
  - Chat
  - Message
  - Notification
  - RefundRequest
  - Wishlist
  - Address

- `DAL.DbContext`: LorKingdomDbContext (Entity Framework Core DbContext)

- `DAL.Interfaces`: Repository contracts
  - IAccountRepository
  - IProductRepository
  - IOrderRepository
  - IGenericRepository<T>
  - ... (other repository interfaces)

**Dependencies:**
- Entity Framework Core
- SQL Server Database

---

### 4. External Services
**Fully Qualified Name:** `ASP_LorKingDom.System.External`

**Purpose:** Integration with third-party services.

**Sub-packages:**
- `External.PaymentGateway`: Payment processing
  - VNPay API Integration
  - Momo API Integration
  
- `External.EmailService`: Email notifications
  - SMTP Service

- `External.StorageService`: File management
  - File Upload/Download

---

## Dependency Relationships

### Primary Dependencies (<<import>>)
1. **WebUI → BLL**: WebUI imports business services
2. **BLL → DAL**: BLL imports data repositories
3. **All Layers → Models**: All layers import entity models

### Access Dependencies (<<access>>)
1. **WebUI → BLL.DTOs**: WebUI accesses DTOs for data transfer
2. **BLL → External Services**: BLL accesses external APIs
3. **Services → Interfaces**: Services implement interfaces

---

## Key Design Principles

### 1. Separation of Concerns
Each layer has a distinct responsibility:
- **WebUI**: User interaction
- **BLL**: Business logic
- **DAL**: Data persistence

### 2. Dependency Inversion
- All layers depend on abstractions (interfaces)
- Concrete implementations are injected via Dependency Injection

### 3. Repository Pattern
- DAL implements repository pattern for data access
- Provides abstraction over data operations

### 4. DTO Pattern
- Data Transfer Objects prevent direct exposure of entities
- Provides data transformation and validation layer

### 5. Service Layer Pattern
- Business logic encapsulated in service classes
- Services coordinate between repositories and controllers

---

## Package Constraints

### Naming Conventions
1. **Unique Package Names**: Each package has a unique name within its parent scope
2. **Namespaces**: Follow C# namespace conventions (ASP_LorKingDom.BLL.Services)

### Visibility Rules
1. **Public Interfaces**: All service and repository interfaces are public
2. **Internal Implementations**: Some implementation details are internal
3. **Private Helpers**: Helper methods are private within services

### Content Constraints
1. **Controllers Package**: Contains only MVC controllers
2. **Models Package**: Contains only entity classes
3. **Services Package**: Contains only business logic services
4. **Repositories Package**: Contains only data access implementations

### Dependency Rules
1. **No Circular Dependencies**: WebUI → BLL → DAL (one-way dependency)
2. **Interface Segregation**: Each service interface focused on single responsibility
3. **No Skip-Level Dependencies**: WebUI cannot directly access DAL

---

## Technology Stack by Package

### WebUI
- ASP.NET Core MVC 6.0+
- SignalR (for real-time chat)
- Razor Views
- Bootstrap 5
- jQuery

### BLL
- C# Business Logic
- FluentValidation
- AutoMapper (for DTO mapping)

### DAL
- Entity Framework Core
- LINQ
- SQL Server

### External
- VNPay SDK
- Momo SDK
- SMTP Client

---

## Deployment Packages

```
┌────────────────────────────────┐
│   Deployment Configuration     │
├────────────────────────────────┤
│                                │
│  WebUI.dll                     │
│  BLL.dll                       │
│  DAL.dll                       │
│  wwwroot/ (static files)       │
│  appsettings.json              │
│  web.config                    │
│                                │
└────────────────────────────────┘
```

---

## Notes

1. **Extensibility**: New features can be added by creating new services and repositories
2. **Testability**: Each layer can be tested independently using mocked dependencies
3. **Maintainability**: Clear separation allows team members to work on different layers
4. **Scalability**: Layers can be scaled independently if needed
5. **Documentation**: Each package should maintain its own documentation

---

## Version History
- **v1.0** (2025-11-05): Initial package diagram creation
- Based on current project structure analysis

---

## Related Diagrams
- Component Diagram
- Class Diagrams (for each package)
- Deployment Diagram
- State Charts (in StateCharts folder)
