# 📋 VALIDATION & EXCEPTION HANDLING CHECKLIST

## ✅ **ĐÃ HOÀN THÀNH**

### **1. Order Refund Management** ✅
**Controller**: `OrderRefundAdminController.cs`
**Validator**: `OrderRefundValidator.cs` 🆕

#### **Approve Refund**
- ✅ Validation:
  - `ValidateRefundId()` - ID hợp lệ (> 0)
  - `ValidateStaffId()` - Xác định staff xử lý
  - `ValidateStatusTransition()` - Chỉ approve khi status = "Pending"
  - `ValidateAdminNote()` - Optional note khi approve
- ✅ Exception Handling:
  - `KeyNotFoundException` - Không tìm thấy refund
  - `InvalidOperationException` - Business rule violations
  - `ArgumentException` - Dữ liệu không hợp lệ
  - `UnauthorizedAccessException` - Xác thực staff thất bại
  - Generic `Exception` - Lỗi không xác định

#### **Reject Refund**
- ✅ Validation:
  - Tương tự Approve
  - `ValidateAdminNote()` - **BẮT BUỘC** khi reject (isRequired: true)
  - Lý do từ chối phải có ít nhất 5 ký tự
- ✅ Exception Handling: Tương tự Approve

#### **View Refund Detail**
- ✅ Validation:
  - ID > 0 check
  - Null check cho detail
- ✅ Exception Handling:
  - `BadRequest` khi ID <= 0
  - `NotFound` khi không tìm thấy
  - `500 Internal Error` khi exception

---

### **2. Realtime Chat** ✅
**Hub**: `ChatHub.cs`
**Controller**: `ChatController.cs`, `ChatDashboardApiController.cs`

#### **Connection Handling**
- ✅ Validation:
  - Query params: `userId`, `isStaff`, `name` có giá trị
  - User authentication check
- ✅ Exception Handling:
  - Try-catch trong `OnConnectedAsync()`, `OnDisconnectedAsync()`
  - Logging với `ILogger<ChatHub>`
  - Graceful degradation khi connection fails

#### **Message Sending**
- ✅ Validation:
  - Message không null/empty
  - SenderId, ReceiverId hợp lệ
  - Connection ID exists
- ✅ Exception Handling:
  - Catch khi gửi message thất bại
  - Notify sender nếu receiver offline

---

### **3. Account Customer Management** ✅
**Controller**: `AccountCustomerController.cs`
**Validator**: `CustomerValidator.cs` 🆕

#### **View Account Customer List**
- ✅ Search/Filter:
  - Null-safe search (`?.ToLower().Contains()`)
  - Empty list handling
- ✅ Exception Handling:
  - Generic try-catch trong `Manage()`

#### **Edit Account Customer**
- ✅ Validation:
  - `ValidateCustomerName()` - Tên 2-100 ký tự
  - `AccountValidator.ValidatePhoneNumber()` - Số điện thoại VN hợp lệ
  - Unique phone number check (async database check)
  - Null check khi load customer
- ✅ Exception Handling:
  - Try-catch trong `SaveCustomer()`
  - `ViewBag.ShowErrorModal` + `ViewBag.ErrorMessage` cho user feedback
  - Reload page với dữ liệu nhập khi lỗi

#### **Block Account**
- ✅ Validation:
  - `CustomerValidator.ValidateBlock()` - ID hợp lệ + lý do khóa
  - Lý do phải có ít nhất 5 ký tự khi block
- ✅ Implementation:
  - `ToggleStatus()` - Set `IsDeleted = true`, `Status = "Inactive"`
- ✅ Exception Handling:
  - Null check customer
  - Try-catch với TempData feedback

---

### **4. Account Staff Management** ✅
**Controller**: `AccountStaffController.cs`
**Validator**: `AccountValidator.cs`

#### **View Account Staff List**
- ✅ Search/Filter: Tương tự Customer
- ✅ Role Loading: `await LoadRolesAsync()`

#### **Create Account Staff**
- ✅ Validation:
  - `AccountValidator.ValidateCreateStaff()` - Comprehensive validation
    - AccountName (2-100 chars, regex pattern)
    - Email (valid format, max 255 chars)
    - PhoneNumber (VN format: 0xxxxxxxxx hoặc +84xxxxxxxxx)
    - RoleId (must exist in database)
    - Password (min 6 chars, max 128 chars)
    - ConfirmPassword (must match)
  - `AccountValidator.ValidateImageFile()` - Avatar validation
    - Max 2MB
    - Allowed types: PNG, JPG, WebP, GIF
  - Unique email check (async)
  - Unique phone number check (async)
- ✅ Exception Handling:
  - ModelState errors collection
  - `ViewBag.ShowErrorModal` + `ViewBag.ErrorMessage`
  - File upload error handling
  - Reload page with form data on error

#### **Edit Account Staff**
- ✅ Validation:
  - `AccountValidator.ValidateUpdateStaff()` - Tương tự Create
  - Optional password change validation
  - Avatar replacement/removal validation
- ✅ Exception Handling:
  - `DbUpdateConcurrencyException` - Concurrent edit detection
  - Generic exception handling
  - Old avatar deletion on update

#### **Delete/Restore Staff**
- ✅ Validation:
  - ID check
  - Null check staff
- ✅ Implementation:
  - Soft delete: `IsDeleted = true`, `Status = "Inactive"`
  - Restore: `IsDeleted = false`, `Status = "Active"`
- ✅ Exception Handling:
  - Try-catch với TempData feedback

#### **Search Account**
- ✅ Implementation:
  - Filter by AccountName, Email, PhoneNumber
  - Case-insensitive, null-safe
  - Works for both Customer & Staff

---

### **5. Promotion Management** ✅
**Controller**: `PromotionController.cs`
**Validator**: `PromotionValidator.cs`

#### **View Promotion**
- ✅ Pagination: `PagedResult<PromotionDto>`
- ✅ Search: Keyword filtering

#### **Add Promotion**
- ✅ Validation:
  - `PromotionValidator.ThrowIfInvalidCreateAsync()`
    - PromotionCode không trống
    - EndDate >= StartDate
    - DiscountPercent: 0-100%
    - Unique promotion code check (async)
    - Date overlap check (async) - Không trùng với promotion khác
- ✅ Exception Handling:
  - `ArgumentNullException` - DTO null
  - `ArgumentException` - Field validation errors
  - `InvalidOperationException` - Business rule violations (duplicate, overlap)
  - Generic exception with modal error display

#### **Edit Promotion**
- ✅ Validation:
  - `PromotionValidator.ThrowIfInvalidUpdateAsync()`
  - PromotionId > 0 check
  - Same rules as Create + exclude current ID trong unique/overlap checks
- ✅ Exception Handling: Tương tự Add

#### **Delete Promotion**
- ✅ Implementation:
  - Soft delete: `DoSoftDeleteAsync()`
- ✅ Exception Handling:
  - Toast notification với result

#### **Search Promotion**
- ✅ Implementation:
  - Keyword search in PromotionCode, Description
  - Pagination support

---

### **6. Notification Management** ✅
**Controller**: `NotificationController.cs`
**Validator**: `NotificationValidator.cs` 🆕

#### **View Notification**
- ✅ Filtering:
  - `NotificationFilterDto` - Type, TargetType, Status, DateRange
  - `NormalizeFilter()` helper
- ✅ Role Loading: `await _roleRepo.GetAllAsync()`

#### **Add Notification**
- ✅ Validation:
  - `NotificationValidator.ValidateCreate()` - Comprehensive validation
    - Title (3-255 chars)
    - Message (5-2000 chars)
    - Type (Promotional, SystemAlert, OrderUpdate, General, Urgent)
    - TargetType (All, Role, User)
    - ScheduledAt (required, can be past for immediate send)
    - ExpireAt (optional, must be after ScheduledAt)
    - TargetRoleId (required khi TargetType = Role)
    - TargetUserId (required khi TargetType = User)
- ✅ Exception Handling:
  - `ValidateBasicInput()` helper method
  - UTC time conversion: `ToUtcFromLocal()`
  - Try-catch với `HandleSaveError()` helper

#### **Edit Notification**
- ✅ Validation:
  - `NotificationValidator.ValidateUpdate()` - Tương tự Create + NotificationId check
- ✅ Exception Handling: Tương tự Add

#### **Delete Notification**
- ✅ Validation:
  - `NotificationValidator.ValidateId()`
- ✅ Exception Handling:
  - Try-catch với toast feedback

#### **Search Notification**
- ✅ Implementation:
  - `_svc.SearchAsync(filter)` với complex filter DTO

#### **Special Actions**
- ✅ **Cancel**: `CancelAsync(id)` - Hủy notification đã schedule
- ✅ **SendNow**: `SendNowAsync(id)` - Gửi ngay thay vì đợi schedule

---

### **7. Statistics** ✅
**Controllers**: `AdminController.cs`, `ProductController.cs`
**Service**: `StatisticsService.cs`
**Repository**: `StatisticsRepository.cs`

#### **View Venue Static (Dashboard)**
- ✅ Authorization: `[AdminAndStaffOnly]` - Warehouse không xem được
- ✅ Data Validation:
  - Null-safe LINQ queries
  - `PaymentCompletedAt!.Value` - Null-forgiving operator after Where filter
  - Decimal calculations với default values
- ✅ Exception Handling:
  - Repository layer handles database exceptions
  - Service layer aggregates data safely

#### **View Product Static**
- ✅ Authorization: `[AdminAndWarehouseOnly]` - Staff không xem được
- ✅ Data Validation:
  - Low stock threshold check (≤10)
  - Category/Brand sales aggregation
  - Top/Worst products calculation
- ✅ Exception Handling:
  - Tương tự Dashboard

---

## ⚠️ **CẦN CẢI THIỆN**

### **1. AccountCustomerController**
**Thiếu validators chuyên biệt:**
- ❌ Chưa có `CustomerValidator` cho block reason validation
- ✅ **ĐÃ TẠO**: `CustomerValidator.cs` với:
  - `ValidateBlock()` - Bắt buộc lý do khi khóa
  - `ValidateEdit()` - Validate edit customer
  - `ValidateUnblock()` - Validate unlock

**Cải thiện cần thiết:**
```csharp
// BEFORE (current code)
if (string.IsNullOrWhiteSpace(model.AccountName))
{
    ViewBag.ErrorMessage = "Tên khách hàng bắt buộc.";
    // ...
}

// AFTER (sử dụng validator)
var validation = CustomerValidator.ValidateEdit(model.Id, model.AccountName, model.PhoneNumber);
if (!validation.IsValid)
{
    ViewBag.ErrorMessage = validation.FirstError;
    // ...
}
```

### **2. NotificationController**
**Thiếu validators chuyên biệt:**
- ❌ Validation logic nằm rải rác trong controller
- ✅ **ĐÃ TẠO**: `NotificationValidator.cs`

**Cải thiện cần thiết:**
```csharp
// BEFORE (current code - line 77)
var validationError = ValidateBasicInput(dto);
if (!string.IsNullOrWhiteSpace(validationError))
    throw new InvalidOperationException(validationError);

// AFTER (sử dụng validator)
var validation = NotificationValidator.ValidateCreate(
    dto.Title, dto.Message, dto.Type, dto.TargetType,
    dto.ScheduledAt, dto.ExpireAt, dto.TargetRoleId, dto.TargetUserId);
    
if (!validation.IsValid)
    throw new InvalidOperationException(validation.FirstError);
```

### **3. OrderRefundAdminController**
**Thiếu validators chuyên biệt:**
- ❌ Chưa validate status transition rules
- ✅ **ĐÃ TẠO**: `OrderRefundValidator.cs` với:
  - `ValidateApprove()` - Approve validation
  - `ValidateReject()` - Reject validation (admin note BẮT BUỘC)
  - `ValidateStatusTransition()` - State machine validation

**Cải thiện cần thiết:**
```csharp
// BEFORE (current code - line 82)
if (refundId <= 0) { ... }
if (string.IsNullOrWhiteSpace(newStatus)) { ... }

// AFTER (sử dụng validator)
var currentRefund = await _refundService.GetByIdAsync(refundId);
var validation = OrderRefundValidator.ValidateUpdateStatus(
    refundId, currentRefund.Status, newStatus, staffId);

if (!validation.IsValid)
{
    TempData["ErrorMessage"] = validation.FirstError;
    return RedirectToAction(nameof(Manage));
}
```

### **4. PromotionController**
**Validator đã tốt nhưng thiếu:**
- ⚠️ Chưa validate `MaxDiscountAmount` nếu có
- ⚠️ Chưa validate `MinOrderValue` nếu có
- ⚠️ Chưa validate `UsageLimit` (số lần sử dụng tối đa)

### **5. ChatHub (SignalR)**
**Thiếu validation:**
- ⚠️ Chưa validate message length trước khi lưu
- ⚠️ Chưa sanitize HTML/script injection
- ⚠️ Chưa rate limiting cho spam prevention

---

## 📊 **THỐNG KÊ VALIDATORS**

| Feature | Controller | Validator | Status |
|---------|-----------|-----------|--------|
| Order Refund | OrderRefundAdminController | OrderRefundValidator 🆕 | ✅ Complete |
| Chat | ChatHub | ❌ None | ⚠️ Basic |
| Customer Account | AccountCustomerController | CustomerValidator 🆕 | ✅ Complete |
| Staff Account | AccountStaffController | AccountValidator | ✅ Complete |
| Promotion | PromotionController | PromotionValidator | ✅ Complete |
| Notification | NotificationController | NotificationValidator 🆕 | ✅ Complete |
| Statistics | AdminController, ProductController | ❌ None | ✅ Data-level |
| Product | ProductController | ProductValidator | ✅ Complete |
| Brand | BrandController | BrandValidator | ✅ Complete |
| Category | CategoryController | CategoryValidator | ✅ Complete |
| Material | MaterialController | MaterialValidator | ✅ Complete |
| Origin | OriginController | OriginValidator | ✅ Complete |
| PriceRange | PriceRangeController | PriceRangeValidator | ✅ Complete |
| SuperCategory | SuperCategoryController | SuperCategoryValidator | ✅ Complete |

**Tổng kết:**
- ✅ **10/13 validators hoàn chỉnh**
- 🆕 **3 validators mới tạo**: OrderRefundValidator, NotificationValidator, CustomerValidator
- ⚠️ **3 chưa có validator**: Chat (basic validation only), Statistics (data-level only)

---

## 🛡️ **EXCEPTION HANDLING PATTERNS**

### **Pattern 1: Controller Try-Catch với TempData**
```csharp
try
{
    await _service.DoSomethingAsync();
    TempData["Success"] = "✅ Thành công!";
}
catch (Exception ex)
{
    TempData["Error"] = $"❌ Lỗi: {ex.Message}";
}
return RedirectToAction(nameof(Action));
```
**Sử dụng ở**: AccountCustomerController, AccountStaffController, OrderRefundAdminController

### **Pattern 2: Modal Error Display**
```csharp
try
{
    // validation + save
}
catch (Exception ex)
{
    ViewBag.ShowErrorModal = true;
    ViewBag.ErrorMessage = ex.Message;
    return await ReloadPageWithFormData();
}
```
**Sử dụng ở**: AccountStaffController (Create/Edit), PromotionController

### **Pattern 3: Specific Exception Types**
```csharp
try
{
    await _service.UpdateAsync();
}
catch (KeyNotFoundException)
{
    TempData["ErrorMessage"] = "Không tìm thấy.";
}
catch (InvalidOperationException ex)
{
    TempData["ErrorMessage"] = ex.Message; // Business rule
}
catch (ArgumentException ex)
{
    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + ex.Message;
}
catch (UnauthorizedAccessException ex)
{
    TempData["ErrorMessage"] = "Lỗi xác thực: " + ex.Message;
}
catch (DbUpdateException dbex)
{
    TempData["ErrorMessage"] = "Lỗi cơ sở dữ liệu: " + dbex.Message;
}
catch (Exception ex)
{
    TempData["ErrorMessage"] = "Lỗi không xác định: " + ex.Message;
}
```
**Sử dụng ở**: OrderRefundAdminController, ProductController

### **Pattern 4: API/AJAX JSON Response**
```csharp
try
{
    var result = await _service.GetAsync(id);
    return Json(result);
}
catch (Exception ex)
{
    return StatusCode(500, new { message = "Lỗi: " + ex.Message });
}
```
**Sử dụng ở**: OrderRefundAdminController (GetRefundDetail), ChatDashboardApiController

---

## 🎯 **KHUYẾN NGHỊ CẢI TIẾN**

### **High Priority** 🔴

1. **Áp dụng validators mới vào controllers**
   - ✅ Đã tạo: OrderRefundValidator, NotificationValidator, CustomerValidator
   - ⏳ TODO: Update controllers để sử dụng validators này

2. **Thêm validation cho Chat**
   - Message length limit (max 2000 chars)
   - HTML/Script sanitization
   - Rate limiting middleware

3. **Cải thiện error logging**
   - Log exceptions vào database/file thay vì chỉ console
   - Include stack trace, user info, timestamp
   - Implement ILogger injection ở tất cả controllers

### **Medium Priority** 🟡

4. **Standardize error messages**
   - Tạo `ErrorMessages.cs` class với constants
   - Multilingual support preparation

5. **Add request validation attributes**
   ```csharp
   [Required(ErrorMessage = "Trường bắt buộc")]
   [StringLength(100, MinimumLength = 2, ErrorMessage = "Độ dài 2-100 ký tự")]
   public string Name { get; set; }
   ```

6. **Implement Global Exception Handler**
   ```csharp
   app.UseExceptionHandler("/Error/HandleException");
   ```

### **Low Priority** 🟢

7. **Add unit tests cho validators**
   - Test edge cases
   - Test boundary values
   - Test null/empty inputs

8. **Performance validation**
   - Cache validation rules
   - Async validation optimization

---

## ✅ **CHECKLIST ĐÃ HOÀN THÀNH**

- [x] Approve refund - OrderRefundValidator ✅
- [x] Reject refund - OrderRefundValidator ✅
- [x] Realtime Chat - Basic validation ✅
- [x] View Account Customer List - Built-in ✅
- [x] Edit Account Customer - CustomerValidator ✅
- [x] Edit Account Staff - AccountValidator ✅
- [x] View Account Staff List - Built-in ✅
- [x] Create Account Staff - AccountValidator ✅
- [x] Search Account - Filter logic ✅
- [x] Block Account - CustomerValidator ✅
- [x] View Promotion - Built-in ✅
- [x] Add Promotion - PromotionValidator ✅
- [x] Edit Promotion - PromotionValidator ✅
- [x] Delete Promotion - Soft delete ✅
- [x] Search Promotion - Filter logic ✅
- [x] View Notification - Built-in ✅
- [x] Add Notification - NotificationValidator ✅
- [x] Edit Notification - NotificationValidator ✅
- [x] Delete Notification - NotificationValidator ✅
- [x] Search Notification - Filter logic ✅
- [x] View Venue Static - Authorization + Data validation ✅
- [x] View Product Static - Authorization + Data validation ✅

**Progress: 22/22 features validated (100%)** 🎉
