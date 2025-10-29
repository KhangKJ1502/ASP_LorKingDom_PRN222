# 📝 Naming Convention - Controller Actions

## 🎯 Mục đích
Chuẩn hóa quy tắc đặt tên action trong các controller để:
- ✅ Dễ đọc, dễ hiểu ý nghĩa
- ✅ Nhất quán trong toàn bộ codebase
- ✅ Tránh nhầm lẫn giữa Create/Update
- ✅ RESTful naming pattern

## 📋 Bảng Quy Tắc Đặt Tên

| Chức năng | Tên Action | HTTP Method | Mô tả |
|-----------|------------|-------------|-------|
| **Thêm mới** | `Create{Entity}` | POST | Tạo entity mới |
| **Cập nhật** | `Update{Entity}` | POST | Cập nhật entity hiện có |
| **Xóa mềm** | `Delete{Entity}` hoặc `SoftDelete` | POST | Đánh dấu IsDeleted = true |
| **Xóa cứng** | `HardDelete{Entity}` | POST | Xóa vĩnh viễn khỏi DB |
| **Khôi phục** | `Restore{Entity}` | POST | Set IsDeleted = false |
| **Duyệt** | `Approve{Entity}` | POST | Approve request (Refund, Order...) |
| **Từ chối** | `Reject{Entity}` | POST | Reject request |
| **Chặn** | `Block{Entity}` | POST | Chặn tài khoản/entity |
| **Bỏ chặn** | `Unblock{Entity}` | POST | Bỏ chặn tài khoản/entity |
| **Lấy danh sách** | `Manage` hoặc `Index` | GET | Hiển thị danh sách với filter |
| **Xem chi tiết** | `Details` hoặc `Get{Entity}Detail` | GET | Xem chi tiết 1 entity |
| **Form tạo mới** | `Add` hoặc `Create` | GET | Hiển thị form thêm mới |
| **Form chỉnh sửa** | `Edit` | GET | Hiển thị form chỉnh sửa |

## ✅ Controllers Đã Chuẩn Hóa

### 1. **OrderRefundAdminController** ✅

**TRƯỚC:**
```csharp
[HttpPost("UpdateStatus")]
public async Task<IActionResult> UpdateStatus(long refundId, string newStatus)
{
    // Dùng chung cho cả Approve, Reject, Processing...
}
```

**SAU:**
```csharp
// ✅ Action riêng cho Approve
[HttpPost("ApproveRefund")]
public async Task<IActionResult> ApproveRefund(long refundId)
{
    await _refundService.ApproveOrUpdateStatusAsync(refundId, "Approved", staffId);
    TempData["SuccessMessage"] = "✅ Đã duyệt yêu cầu hoàn tiền.";
}

// ✅ Action riêng cho Reject
[HttpPost("RejectRefund")]
public async Task<IActionResult> RejectRefund(long refundId)
{
    await _refundService.ApproveOrUpdateStatusAsync(refundId, "Rejected", staffId);
    TempData["SuccessMessage"] = "❌ Đã từ chối yêu cầu hoàn tiền.";
}

// ✅ Giữ lại UpdateStatus cho các trạng thái khác (Processing, Completed, Cancelled)
[HttpPost("UpdateStatus")]
public async Task<IActionResult> UpdateStatus(long refundId, string newStatus)
```

**Lợi ích:**
- Tên action rõ ràng: `ApproveRefund`, `RejectRefund`
- Validate riêng cho từng loại (Reject cần adminNote)
- Message thành công cụ thể

---

### 2. **AccountCustomerController** ✅

**TRƯỚC:**
```csharp
[HttpPost]
public async Task<IActionResult> ToggleStatus(int id, string? q)
{
    c.IsDeleted = !c.IsDeleted; // Toggle true/false
}
```

**SAU:**
```csharp
// ✅ Action riêng cho Block
[HttpPost]
public async Task<IActionResult> BlockAccount(int id, string? q)
{
    if (c.IsDeleted)
    {
        TempData["Error"] = "Khách hàng đã bị chặn rồi!";
        return RedirectToAction(nameof(Manage), new { q });
    }
    
    c.IsDeleted = true;
    c.Status = "Inactive";
    TempData["Success"] = "🚫 Đã chặn khách hàng thành công!";
}

// ✅ Action riêng cho Unblock
[HttpPost]
public async Task<IActionResult> UnblockAccount(int id, string? q)
{
    if (!c.IsDeleted)
    {
        TempData["Error"] = "Khách hàng chưa bị chặn!";
        return RedirectToAction(nameof(Manage), new { q });
    }
    
    c.IsDeleted = false;
    c.Status = "Active";
    TempData["Success"] = "✅ Đã bỏ chặn khách hàng thành công!";
}

// ⚠️ DEPRECATED - Giữ lại để tương thích ngược
[Obsolete("Use BlockAccount() or UnblockAccount() instead")]
public async Task<IActionResult> ToggleStatus(int id, string? q)
```

**Lợi ích:**
- Tên action rõ ràng: `BlockAccount`, `UnblockAccount`
- Validate state trước khi thực hiện
- Message thành công cụ thể
- Không thể block 2 lần (idempotent)

---

### 3. **NotificationController** ✅

**TRƯỚC:**
```csharp
[HttpPost]
public async Task<IActionResult> SaveNotification(NotificationSaveDto dto)
{
    if (dto.NotificationId.HasValue && dto.NotificationId.Value > 0)
    {
        // UPDATE logic
    }
    else
    {
        // CREATE logic
    }
}
```

**SAU:**
```csharp
// ✅ Action riêng cho Create
[HttpPost]
public async Task<IActionResult> CreateNotification(NotificationSaveDto dto)
{
    await _svc.CreateAsync(new NotificationCreateDto { ... });
    TempData["Success"] = "✅ Thêm thông báo mới thành công.";
}

// ✅ Action riêng cho Update
[HttpPost]
public async Task<IActionResult> UpdateNotification(NotificationSaveDto dto)
{
    if (!dto.NotificationId.HasValue || dto.NotificationId.Value <= 0)
        throw new InvalidOperationException("ID thông báo không hợp lệ.");
    
    await _svc.UpdateAsync(new NotificationUpdateDto { ... });
    TempData["Success"] = "✅ Cập nhật thông báo thành công.";
}

// ⚠️ DEPRECATED
[Obsolete("Use CreateNotification() or UpdateNotification() instead")]
public async Task<IActionResult> SaveNotification(NotificationSaveDto dto)
```

**Error Handling:**
```csharp
// CreateNotification error → Mở Add Modal
private async Task<IActionResult> HandleCreateError(Exception ex, NotificationSaveDto dto)
{
    ViewBag.ShowAddModal = true;
    ViewBag.EditNotification = null;
}

// UpdateNotification error → Mở Edit Modal
private async Task<IActionResult> HandleUpdateError(Exception ex, NotificationSaveDto dto)
{
    ViewBag.EditNotification = new NotificationDto { ... };
}
```

---

### 4. **PromotionController** ✅

**TRƯỚC:**
```csharp
[HttpPost]
public async Task<IActionResult> SavePromotion(PromotionSaveDto dto)
{
    if (dto.PromotionId > 0)
    {
        // UPDATE
        await _promotionService.UpdateAsync(...);
    }
    else
    {
        // CREATE
        await _promotionService.CreateAsync(...);
    }
}
```

**SAU:**
```csharp
// ✅ Action riêng cho Create
[HttpPost]
public async Task<IActionResult> CreatePromotion(PromotionSaveDto dto)
{
    var created = await _promotionService.CreateAsync(new PromotionCreateDto { ... });
    TempData["Success"] = "✅ Thêm khuyến mãi mới thành công.";
}

// ✅ Action riêng cho Update
[HttpPost]
public async Task<IActionResult> UpdatePromotion(PromotionSaveDto dto)
{
    if (dto.PromotionId <= 0)
        throw new InvalidOperationException("ID khuyến mãi không hợp lệ.");
    
    var ok = await _promotionService.UpdateAsync(new PromotionUpdateDto { ... });
    if (!ok)
        throw new InvalidOperationException("Cập nhật thất bại hoặc không tìm thấy.");
    
    TempData["Success"] = "✅ Cập nhật khuyến mãi thành công.";
}

// ⚠️ DEPRECATED
[Obsolete("Use CreatePromotion() or UpdatePromotion() instead")]
public async Task<IActionResult> SavePromotion(PromotionSaveDto dto)
```

**Error Handling:**
```csharp
// CreatePromotion error → Mở Add Modal
ViewBag.ShowAddModal = true;
ViewBag.EditPromotion = null;

// UpdatePromotion error → Mở Edit Modal
ViewBag.EditPromotion = BuildPromotionDtoFromForm(dto);
```

---

### 5. **AccountStaffController** ✅ (Đã đúng từ trước)

```csharp
// ✅ Create action
[HttpPost]
public async Task<IActionResult> CreateStaff(AccountDto model, string? q)
{
    model.Id = 0; // Force create
    // Validation + Create logic
}

// ✅ Update action
[HttpPost]
public async Task<IActionResult> UpdateStaff(AccountDto model, string? q)
{
    if (model.Id <= 0)
        throw new InvalidOperationException("ID không hợp lệ cho cập nhật.");
    // Validation + Update logic
}
```

---

## ⚠️ Controllers CẦN CHUẨN HÓA (TODO)

### Controllers vẫn dùng `Save{Entity}`:

| Controller | Action hiện tại | Nên đổi thành |
|-----------|----------------|---------------|
| **ProductController** | `Save()` | `CreateProduct()` + `UpdateProduct()` |
| **BrandController** | `SaveBrand()` | `CreateBrand()` + `UpdateBrand()` |
| **CategoryController** | `SaveCategory()` | `CreateCategory()` + `UpdateCategory()` |
| **MaterialController** | `SaveMaterial()` | `CreateMaterial()` + `UpdateMaterial()` |
| **OriginController** | `SaveOrigin()` | `CreateOrigin()` + `UpdateOrigin()` |
| **SuperCategoryController** | `SaveSuperCategory()` | `CreateSuperCategory()` + `UpdateSuperCategory()` |
| **PriceRangeController** | `SavePriceRange()` | `CreatePriceRange()` + `UpdatePriceRange()` |

### Controllers vẫn dùng `Edit()` cho POST:

| Controller | Action hiện tại | Nên đổi thành |
|-----------|----------------|---------------|
| **AccountCustomerController** | `SaveCustomer()` | `UpdateCustomer()` (không có Create) |

---

## 🔄 Migration Strategy

### Để đảm bảo backward compatibility:

1. **Thêm action mới** (Create/Update) với logic riêng biệt
2. **Đánh dấu action cũ** với `[Obsolete("...")]`
3. **Giữ lại action cũ** để views cũ vẫn hoạt động
4. **Update views** sử dụng action mới
5. **Xóa action cũ** sau khi đã update hết views

### Ví dụ Migration:

```csharp
// STEP 1: Thêm action mới
[HttpPost]
public async Task<IActionResult> CreatePromotion(PromotionSaveDto dto) { ... }

[HttpPost]
public async Task<IActionResult> UpdatePromotion(PromotionSaveDto dto) { ... }

// STEP 2: Đánh dấu action cũ
[HttpPost]
[Obsolete("Use CreatePromotion() or UpdatePromotion() instead", false)]
public async Task<IActionResult> SavePromotion(PromotionSaveDto dto) { ... }

// STEP 3: Update view để dùng action mới
// Before: <form asp-action="SavePromotion">
// After:  <form asp-action="CreatePromotion"> hoặc <form asp-action="UpdatePromotion">

// STEP 4: Sau khi test xong, xóa action cũ
// (Xóa SavePromotion)
```

---

## 📊 Tổng Kết

### ✅ Đã hoàn thành:
- ✅ OrderRefundAdminController (ApproveRefund, RejectRefund)
- ✅ AccountCustomerController (BlockAccount, UnblockAccount)
- ✅ NotificationController (CreateNotification, UpdateNotification)
- ✅ PromotionController (CreatePromotion, UpdatePromotion)
- ✅ AccountStaffController (CreateStaff, UpdateStaff) - Đã đúng từ trước

### ⏳ Chưa hoàn thành (8 controllers):
- ProductController
- BrandController
- CategoryController
- MaterialController
- OriginController
- SuperCategoryController
- PriceRangeController
- AccountCustomerController (chỉ có Update, thiếu Create)

### 📈 Progress: 5/13 controllers (38%)

---

## 🎯 Best Practices

### ✅ DO:
- ✅ Dùng `Create{Entity}` cho thêm mới
- ✅ Dùng `Update{Entity}` cho cập nhật
- ✅ Dùng `Delete{Entity}` hoặc `SoftDelete` cho xóa
- ✅ Dùng `Approve{Entity}`, `Reject{Entity}` cho workflow
- ✅ Dùng `Block{Entity}`, `Unblock{Entity}` cho account management
- ✅ Validate riêng cho từng action (Create validate khác Update)
- ✅ Error handling riêng (CreateError vs UpdateError)
- ✅ Success message cụ thể

### ❌ DON'T:
- ❌ Dùng `Save{Entity}` gộp chung Create + Update
- ❌ Dùng `ToggleStatus` cho Block/Unblock (không rõ ràng)
- ❌ Dùng `UpdateStatus` cho Approve/Reject (quá chung chung)
- ❌ Check `id == 0` trong controller để phân biệt Create/Update
- ❌ Dùng 1 error handler chung cho cả Create và Update

---

## 📝 Checklist cho việc Refactor Controller

Khi refactor controller từ `Save{Entity}` → `Create{Entity}` + `Update{Entity}`:

- [ ] Tạo action `Create{Entity}` với validation riêng
- [ ] Tạo action `Update{Entity}` với validation riêng
- [ ] Tạo `HandleCreateError()` helper (mở Add modal)
- [ ] Tạo `HandleUpdateError()` helper (mở Edit modal)
- [ ] Đánh dấu `Save{Entity}` với `[Obsolete]`
- [ ] Update view: form thêm mới gọi `Create{Entity}`
- [ ] Update view: form chỉnh sửa gọi `Update{Entity}`
- [ ] Test Create với data hợp lệ
- [ ] Test Create với data lỗi (validate)
- [ ] Test Update với data hợp lệ
- [ ] Test Update với data lỗi (validate)
- [ ] Xóa action `Save{Entity}` sau khi test xong

---

**Last Updated:** October 29, 2025  
**Status:** 5/13 controllers refactored (38%)  
**Next Target:** ProductController, BrandController, CategoryController
