# ASP_LorKingDom - AI Agent Instructions

## 🏗️ Architecture Overview

**3-Layer ASP.NET Core MVC** (Database-First approach with EF Core):
- **WebUI** (ASP_LorKingDom): Controllers, Views (Razor), SignalR Hubs, Background Workers
- **BLL** (Business Logic Layer): Services, DTOs, Validators, Interfaces
- **DAL** (Data Access Layer): Repositories, EF Models (`AspLorKingDomContext`), Interfaces

**Key Pattern**: Repository + Service Pattern with Dependency Injection configured via extension methods (`AddDAL()`, `AddBLL()`).

## 🔐 Authentication & Authorization

**Dual Cookie Authentication** (configured in `Program.cs`):
- **Customer**: `CookieAuthenticationDefaults.AuthenticationScheme` → `/Auth/Login`
- **Admin/Staff**: `"AdminScheme"` → `/AdminAuth/Login`

**Custom Authorization Attributes** (in `Filters/AdminAuthorizationAttributes.cs`):
- `[AdminOnly]` - Admin only
- `[AdminAndStaffOnly]` - Admin + Staff (excludes Warehouse) - **Used for Dashboard/Revenue statistics**
- `[AdminAndWarehouseOnly]` - Admin + Warehouse (excludes Staff) - **Used for Product Statistics**
- `[ManagementOnly]` - Admin + Staff + Warehouse

**Statistics Access Control**:
- **Dashboard (Revenue/Orders)**: Admin and Staff only (`[AdminAndStaffOnly]`) - Warehouse không được xem
- **Product Statistics**: Admin and Warehouse only (`[AdminAndWarehouseOnly]`) - Staff không được xem
- **Admin**: Full access to both Dashboard and Product Statistics

Claims use `ClaimTypes.NameIdentifier` for AccountId and `ClaimTypes.Role` for role checking.

## 📦 Service Registration Pattern

**ALL services must be registered in extension methods**:
- DAL repos: `DAL/DALServiceCollectionExtensions.cs` → `AddDAL(connectionString)`
- BLL services: `BLL/BLLServiceCollectionExtensions.cs` → `AddBLL()`

**Example**: When adding new feature (e.g., `IOrderService`):
1. Create `DAL/Interfaces/IOrderRepository.cs` + `DAL/Repositories/OrderRepository.cs`
2. Register in `DALServiceCollectionExtensions`: `services.AddScoped<IOrderRepository, OrderRepository>();`
3. Create `BLL/Interfaces/IOrderService.cs` + `BLL/Services/OrderService.cs`
4. Register in `BLLServiceCollectionExtensions`: `services.AddScoped<IOrderService, OrderService>();`
5. Inject via constructor in controllers

## 🗄️ Database Context

**DbContext**: `DAL/Models/AspLorKingDomContext.cs` - configured with SQL Server
- Connection string in `appsettings.json` (team members have different local servers commented out)
- Models are auto-generated from database (Database-First)
- Use `.Include()` for eager loading navigation properties (see `ProductRepository.GetByIdAsync()`)

**Soft Delete Pattern**: Most entities have `IsDeleted` flag instead of hard deletes
- Products set `IsDeleted = true` and `ProductStatus = "Discontinued"` when parent entities (Brand/Category/Material/Origin) are disabled

## 🎯 Core Domain Patterns

### Product Management
**Unique SKU Generation**: Uses SHA256 hash of timestamp + GUID in `ProductService.GenerateUniqueSkuAsync()`
**Required FK fields for new products**: CategoryId, BrandId, SexId, AgeId, MaterialId, OriginId, PriceRangeId (validated in `ProductValidator.ValidateForCreate()`)
**Image handling**: Multi-image support via `ProductImageService` (MainImageUrl + SecondaryImageUrls list)

### Statistics & Analytics
**Dashboard Statistics** (`StatisticsService.GetDashboardStatisticsAsync()`):
- Revenue metrics (total, today, this month, this year)
- Order counts and status breakdown
- Customer growth tracking
- Top selling products (top 5)
- Daily revenue trend (last 7 days)
- Monthly revenue chart (last 12 months)

**Product Statistics** (`StatisticsService.GetProductStatisticsAsync()`):
- Product counts by status (Active, OutOfStock, Discontinued)
- Low stock alerts (threshold ≤10 items)
- Total inventory value calculation
- Sales analytics by Category and Brand
- Top/Worst performing products (top 10 each)
- Stock alert details with SKU tracking

**Implementation Notes**:
- Uses `StatisticsRepository` with complex LINQ queries
- Field names: `OrderDetail.UnitPrice` (not `PriceAtPurchase`)
- Null-safe navigation with `!` operator for verified non-null paths
- Chart.js integration for data visualization

### Real-time Features
**SignalR ChatHub** (`ChatHubs/ChatHub.cs`):
- Staff/Customer chat with connection tracking (`_staffConnCount`, `_staffPageState`)
- Query params: `userId`, `isStaff`, `name`
- Groups: `user:{userId}` for targeted messaging
- Events: `presenceChanged`, `staffPresence`, `conversationList`

**Background Services**: `NotificationWorkerService` runs every 1 minute to dispatch scheduled notifications

## 🛠️ Common Controller Patterns

**DTO Mapping**: Controllers receive DTOs from services, entities never leave DAL layer
**ModelState + Toast Feedback**: On validation errors, set `ViewBag.ErrorMessage` + `ViewBag.ShowErrorModal = true` to trigger client-side toast
**TempData for Redirects**: Use `TempData["Success"]` / `TempData["Error"]` for messages after PRG pattern

## 📛 Naming Convention for Controller Actions

**MUST follow standardized naming pattern** - See `.github/NAMING_CONVENTION.md` for full documentation.

### Action Naming Rules:

| Chức năng | Tên Action | HTTP | Ví dụ |
|-----------|------------|------|-------|
| **Thêm mới** | `Create{Entity}` | POST | `CreateStaff`, `CreatePromotion`, `CreateNotification` |
| **Cập nhật** | `Update{Entity}` | POST | `UpdateStaff`, `UpdatePromotion`, `UpdateNotification` |
| **Xóa** | `Delete{Entity}` hoặc `SoftDelete` | POST | `DeleteStaff`, `SoftDelete` |
| **Duyệt** | `Approve{Entity}` | POST | `ApproveRefund` |
| **Từ chối** | `Reject{Entity}` | POST | `RejectRefund` |
| **Chặn** | `Block{Entity}` | POST | `BlockAccount` |
| **Bỏ chặn** | `Unblock{Entity}` | POST | `UnblockAccount` |
| **Danh sách** | `Manage` hoặc `Index` | GET | `Manage`, `Index` |
| **Chi tiết** | `Details` hoặc `Get{Entity}Detail` | GET | `GetRefundDetail` |

**✅ DO:**
- Tách riêng `Create{Entity}` và `Update{Entity}` - KHÔNG dùng chung `Save{Entity}`
- Validate riêng cho Create và Update (Create không cần check ID, Update phải check ID > 0)
- Error handling riêng: `HandleCreateError()` mở Add modal, `HandleUpdateError()` mở Edit modal
- Success messages cụ thể cho từng action

**❌ DON'T:**
- Dùng `Save{Entity}` gộp chung Create + Update (gây nhầm lẫn modal)
- Dùng `ToggleStatus` cho Block/Unblock (không rõ ràng)
- Dùng `UpdateStatus` cho Approve/Reject (quá chung chung)
- Check `id == 0` trong action để phân biệt Create/Update

**Controllers đã chuẩn hóa:**
- ✅ `AccountStaffController`: `CreateStaff()` + `UpdateStaff()`
- ✅ `OrderRefundAdminController`: `ApproveRefund()` + `RejectRefund()`
- ✅ `AccountCustomerController`: `BlockAccount()` + `UnblockAccount()`
- ✅ `NotificationController`: `CreateNotification()` + `UpdateNotification()`
- ✅ `PromotionController`: `CreatePromotion()` + `UpdatePromotion()`

**Khi refactor controller cũ:** Đánh dấu action cũ với `[Obsolete("Use Create{Entity}() or Update{Entity}() instead")]`, giữ lại để tương thích ngược, sau đó update views và xóa action cũ.

## ✅ Validation & Exception Handling

**Validators** (in `BLL/Validators/`):
- `AccountValidator` - Staff account creation/update (name, email, phone, password, role, avatar)
- `CustomerValidator` - Customer account management (edit, block/unblock)
- `ProductValidator` - Product CRUD validation
- `PromotionValidator` - Promotion overlap & business rules
- `NotificationValidator` - Notification create/update (title, message, type, target, schedule)
- `OrderRefundValidator` - Refund approve/reject with status transition rules
- `BrandValidator`, `CategoryValidator`, `MaterialValidator`, `OriginValidator`, `PriceRangeValidator`, `SuperCategoryValidator`

**Validation Pattern**:
```csharp
var validation = SomeValidator.ValidateCreate(params...);
if (!validation.IsValid)
{
    ViewBag.ShowErrorModal = true;
    ViewBag.ErrorMessage = validation.FirstError; // or string.Join("; ", validation.Errors)
    return await ReloadPageWithFormData();
}
```

**Exception Handling Patterns**:
1. **TempData Pattern** - For redirects after POST
   ```csharp
   try {
       await _service.DoAsync();
       TempData["Success"] = "✅ Thành công!";
   } catch (Exception ex) {
       TempData["Error"] = $"❌ {ex.Message}";
   }
   return RedirectToAction(nameof(Action));
   ```

2. **Modal Error Pattern** - For same-page errors with form data retention
   ```csharp
   try {
       // validation + save
   } catch (Exception ex) {
       ViewBag.ShowErrorModal = true;
       ViewBag.ErrorMessage = ex.Message;
       return await ReloadPageWithFormData();
   }
   ```

3. **Specific Exception Types** - For granular error handling
   ```csharp
   try {
       await _service.UpdateAsync();
   } catch (KeyNotFoundException) {
       TempData["ErrorMessage"] = "Không tìm thấy.";
   } catch (InvalidOperationException ex) {
       TempData["ErrorMessage"] = ex.Message; // Business rule
   } catch (ArgumentException ex) {
       TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + ex.Message;
   } catch (UnauthorizedAccessException ex) {
       TempData["ErrorMessage"] = "Lỗi xác thực: " + ex.Message;
   } catch (DbUpdateException dbex) {
       TempData["ErrorMessage"] = "Lỗi database: " + dbex.Message;
   } catch (Exception ex) {
       TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
   }
   ```

**Key Validation Rules**:
- **Refund Status Transition**: Pending → Approved/Rejected/Processing; Approved → Processing/Completed/Cancelled; Completed/Cancelled = final states
- **Notification Targets**: When TargetType=Role, TargetRoleId required; When TargetType=User, TargetUserId required
- **Phone Numbers**: VN format 0xxxxxxxxx or +84xxxxxxxxx (10 digits), valid prefixes checked
- **Passwords**: Min 6 chars, max 128 chars, confirm password must match
- **Images**: Max 2MB, allowed types: PNG/JPG/WebP/GIF

See `.github/VALIDATION_CHECKLIST.md` for comprehensive validation & exception handling documentation.

## 📁 File Upload Convention

**Upload path**: `wwwroot/assets/Component/img_product/` (hardcoded as `SUB` constant)
**Helper**: Controllers use `SaveFileAsync(IFormFile, IWebHostEnvironment, subfolder)` method pattern
**File naming**: `{Guid}_{originalFileName}` to prevent collisions

## 🎨 View Conventions

**Admin views**: Located in `Views/Admin/` (e.g., `ManageProduct.cshtml`, `AddProduct.cshtml`, `Dashboard.cshtml`, `ProductStatistics.cshtml`)
**Shared layouts**: `_Layout.cshtml` (customer), `_AdminLayout.cshtml` (admin panel)
**Partial views**: Prefix with `_` (e.g., `_ProductGridPartial.cshtml`, `_ChatWidget.cshtml`)
**AJAX patterns**: Controllers return `PartialView()` for dynamic content (see `HomeController.ProductGrid()`)

**Chart.js Integration**:
- Loaded via CDN: `https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js`
- Charts in `@section Scripts {}` to load after page content
- Use `@Html.Raw(Json.Serialize(...))` for C# to JS data transfer

## ⚙️ Configuration Notes

**Anti-forgery**: Custom header `RequestVerificationToken` (configured in `Program.cs`)
**Session**: 30-minute timeout, HttpOnly cookies
**SignalR endpoint**: `/chatHub`
**Logging**: Console provider, Debug level minimum

## 🚨 Error Handling Strategy

- `DbUpdateException` caught for database conflicts (duplicate keys, FK violations)
- Business rules throw `ArgumentException` with user-friendly messages
- `ModelState.AddModelError()` for field-level validation errors
- 404 returns from services as `null`, controllers check and redirect

## 📚 Key Dependencies

- ASP.NET Core 8 MVC
- Entity Framework Core (SQL Server provider)
- SignalR for real-time chat
- Cookie-based authentication (dual schemes)
- IFormFile for file uploads
- Chart.js for data visualization

---

**When making changes**: Always update both interface + implementation, register in DI extensions, follow DTO pattern, maintain soft-delete logic for cascading operations. For statistics, ensure proper null-safe navigation and use correct field names from DB models.
