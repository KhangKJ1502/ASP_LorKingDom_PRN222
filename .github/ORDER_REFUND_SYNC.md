# 🔄 Order & OrderRefund Status Synchronization

## 📋 Mục Đích

Đồng bộ hóa trạng thái giữa **Order** và **OrderRefund** để đảm bảo:
- ✅ Khi hoàn tiền được **Approve** → Update Order.RefundStatus = "Approved"
- ✅ Khi hoàn tiền **Completed (Refunded)** → Order chuyển về **"Cancelled"**
- ✅ Tracking đầy đủ lịch sử thay đổi trạng thái qua **OrderStatusHistory**

---

## 🔗 Database Schema

### Order Table
```sql
Order {
    OrderId INT PK
    AccountId INT FK
    StatusId INT FK → StatusOrder.StatusId
    RefundStatus VARCHAR(50)  -- "None", "Pending", "Approved", "Rejected", "Processing", "Refunded"
    ...
}
```

### OrderRefund Table
```sql
OrderRefund {
    RefundId BIGINT PK
    OrderId INT FK → Order.OrderId
    RefundStatus VARCHAR(50)  -- "Requested", "Approved", "Rejected", "Processing", "Refunded", "Cancelled"
    ApprovedBy INT FK (StaffAccountId)
    ApprovedAt DATETIME
    ProcessedAt DATETIME
    ...
}
```

### StatusOrder Table (Lookup)
```sql
StatusOrder {
    StatusId INT PK
    StatusName VARCHAR(50)  -- "Pending", "Confirmed", "Shipping", "Delivered", "Cancelled"
    ...
}
```

### OrderStatusHistory Table (Audit Trail)
```sql
OrderStatusHistory {
    OrderStatusHistoryId INT PK
    OrderId INT FK
    StatusId INT FK
    ChangedAt DATETIME
    ChangedBy INT FK (AccountId)
    Note VARCHAR(500)
    ...
}
```

---

## 🔄 Status Flow Diagram

### OrderRefund Status Flow
```
[Customer]                [Staff]                  [System]
    |                        |                          |
    | Request Refund         |                          |
    |----------------------->|                          |
    | Status: "Requested"    |                          |
    |                        |                          |
    |                   [Approve]                       |
    |                        |------------------------->|
    |                        | Status: "Approved"       |
    |                        |                          |
    |                        |   Update Order           |
    |                        |   RefundStatus="Approved"|
    |                        |                          |
    |                   [Processing]                    |
    |                        |------------------------->|
    |                        | Status: "Processing"     |
    |                        |                          |
    |                        |   Update Order           |
    |                        |   RefundStatus="Processing"|
    |                        |                          |
    |                   [Complete Refund]               |
    |                        |------------------------->|
    |                        | Status: "Refunded"       |
    |                        |                          |
    |                        |   Update Order           |
    |                        |   - StatusId = Cancelled |
    |                        |   - RefundStatus="Refunded"|
    |                        |   - Create StatusHistory |
```

### Order Status Updates
```
Initial Order Status: Pending/Confirmed/Shipping/Delivered
                               |
                   [Customer Request Refund]
                               |
                    OrderRefund.Status = "Requested"
                    Order.RefundStatus = "Pending" (set by service)
                               |
                +--------------+--------------+
                |                             |
        [Staff Approve]               [Staff Reject]
                |                             |
    OrderRefund.Status="Approved"   OrderRefund.Status="Rejected"
    Order.RefundStatus="Approved"   Order.RefundStatus="Rejected"
                |
        [Staff Processing]
                |
    OrderRefund.Status="Processing"
    Order.RefundStatus="Processing"
                |
        [Complete Refund]
                |
    OrderRefund.Status="Refunded"
    Order.StatusId = Cancelled (StatusOrder lookup)
    Order.RefundStatus = "Refunded"
    OrderStatusHistory.Add(note: "Đơn hàng bị hủy do hoàn tiền thành công")
```

---

## 🛠️ Implementation Details

### File Modified: `DAL/Repositories/OrderRefundRepository.cs`

**Method:** `UpdateStatusAsync(long refundId, string newStatus, int staffAccountId)`

#### ✅ Logic Updates:

1. **When Refund Status = "Approved"**
   ```csharp
   if (newStatus == "Approved" && refund.Order != null)
   {
       refund.Order.RefundStatus = "Approved";
       refund.Order.UpdatedAt = DateTime.Now;
   }
   ```

2. **When Refund Status = "Rejected"**
   ```csharp
   if (newStatus == "Rejected" && refund.Order != null)
   {
       refund.Order.RefundStatus = "Rejected";
       refund.Order.UpdatedAt = DateTime.Now;
   }
   ```

3. **When Refund Status = "Processing"**
   ```csharp
   if (newStatus == "Processing" && refund.Order != null)
   {
       refund.Order.RefundStatus = "Processing";
       refund.Order.UpdatedAt = DateTime.Now;
   }
   ```

4. **When Refund Status = "Refunded" (COMPLETED)**
   ```csharp
   if (newStatus == "Refunded")
   {
       refund.ProcessedAt = DateTime.Now;
       
       if (refund.Order != null)
       {
           // Find "Cancelled" StatusId
           var cancelledStatus = await _context.StatusOrders
               .FirstOrDefaultAsync(s => s.StatusName == "Cancelled");
           
           if (cancelledStatus != null)
           {
               // Update Order Status to Cancelled
               refund.Order.StatusId = cancelledStatus.StatusId;
               refund.Order.RefundStatus = "Refunded";
               refund.Order.UpdatedAt = DateTime.Now;
               
               // Create StatusHistory for audit trail
               var statusHistory = new OrderStatusHistory
               {
                   OrderId = refund.OrderId,
                   StatusId = cancelledStatus.StatusId,
                   ChangedAt = DateTime.Now,
                   ChangedBy = staffAccountId,
                   Note = $"Đơn hàng bị hủy do hoàn tiền thành công (RefundId: {refundId})",
                   CreatedAt = DateTime.Now
               };
               _context.OrderStatusHistories.Add(statusHistory);
           }
       }
   }
   ```

---

## 📊 Status Mapping Table

| OrderRefund.RefundStatus | Order.StatusId | Order.RefundStatus | Action Taken |
|--------------------------|----------------|--------------------|--------------|
| **Requested** | (Unchanged) | Pending | Customer submits refund request |
| **Approved** | (Unchanged) | Approved | Staff approves refund request |
| **Rejected** | (Unchanged) | Rejected | Staff rejects refund request |
| **Processing** | (Unchanged) | Processing | Staff is processing the refund |
| **Refunded** | **→ Cancelled** | Refunded | ✅ **Order cancelled + StatusHistory created** |

---

## 🧪 Test Scenarios

### Scenario 1: Full Refund Flow (Happy Path)
```
1. Customer places order
   - Order.StatusId = 1 (Pending)
   - Order.RefundStatus = "None"

2. Customer requests refund
   - OrderRefund.RefundStatus = "Requested"
   - Order.RefundStatus = "Pending" (set by CreateRefundRequestAsync)

3. Staff approves refund
   - POST /OrderRefundAdmin/ApproveRefund
   - OrderRefund.RefundStatus = "Approved"
   - Order.RefundStatus = "Approved" ✅

4. Staff marks as processing
   - POST /OrderRefundAdmin/UpdateStatus (newStatus="Processing")
   - OrderRefund.RefundStatus = "Processing"
   - Order.RefundStatus = "Processing" ✅

5. Staff completes refund
   - POST /OrderRefundAdmin/UpdateStatus (newStatus="Refunded")
   - OrderRefund.RefundStatus = "Refunded"
   - Order.StatusId = 5 (Cancelled) ✅
   - Order.RefundStatus = "Refunded" ✅
   - OrderStatusHistory record created ✅
```

### Scenario 2: Rejected Refund
```
1. Customer requests refund
   - OrderRefund.RefundStatus = "Requested"

2. Staff rejects refund
   - POST /OrderRefundAdmin/RejectRefund
   - OrderRefund.RefundStatus = "Rejected"
   - Order.RefundStatus = "Rejected" ✅
   - Order.StatusId = (Unchanged - still active status)
```

### Scenario 3: Query OrderStatusHistory
```sql
SELECT 
    osh.OrderStatusHistoryId,
    osh.OrderId,
    so.StatusName,
    osh.ChangedAt,
    a.AccountName AS ChangedByName,
    osh.Note
FROM OrderStatusHistory osh
INNER JOIN StatusOrder so ON osh.StatusId = so.StatusId
LEFT JOIN Account a ON osh.ChangedBy = a.AccountId
WHERE osh.OrderId = 123
ORDER BY osh.ChangedAt DESC;
```

**Expected Output for Refunded Order:**
```
OrderStatusHistoryId | OrderId | StatusName | ChangedAt           | ChangedByName | Note
---------------------|---------|------------|---------------------|---------------|---------------------------------------
15                   | 123     | Cancelled  | 2025-10-29 14:30:00 | Admin         | Đơn hàng bị hủy do hoàn tiền thành công (RefundId: 45)
14                   | 123     | Confirmed  | 2025-10-28 10:15:00 | Staff001      | Xác nhận đơn hàng
13                   | 123     | Pending    | 2025-10-28 09:00:00 | System        | Tạo đơn hàng mới
```

---

## 🔍 Validation & Error Handling

### State Machine Validation (in `OrderRefundService.cs`)
```csharp
// From "Requested" → can only go to "Approved" or "Rejected"
if (current.RefundStatus == "Requested")
{
    if (newStatus != "Approved" && newStatus != "Rejected")
        throw new InvalidOperationException("Từ trạng thái 'Yêu cầu', chỉ có thể chuyển sang 'Duyệt' hoặc 'Từ chối'.");
}

// From "Approved" → can only go to "Processing" or "Refunded"
else if (current.RefundStatus == "Approved")
{
    if (newStatus != "Refunded" && newStatus != "Processing")
        throw new InvalidOperationException("Từ trạng thái 'Đã duyệt', chỉ có thể chuyển sang 'Đang xử lý' hoặc 'Đã hoàn tiền'.");
}

// From "Processing" → can only go to "Refunded"
else if (current.RefundStatus == "Processing")
{
    if (newStatus != "Refunded")
        throw new InvalidOperationException("Từ trạng thái 'Đang xử lý', chỉ có thể chuyển sang 'Đã hoàn tiền'.");
}

// Final states: "Rejected", "Refunded", "Cancelled" cannot be changed
else
{
    throw new InvalidOperationException($"Yêu cầu hoàn tiền ở trạng thái '{current.RefundStatus}' không thể thay đổi.");
}
```

### Null Safety
```csharp
if (refund == null)
    throw new KeyNotFoundException($"Refund ID {refundId} not found.");

if (refund.Order != null) // Always check before updating Order
{
    // Safe to update Order properties
}

var cancelledStatus = await _context.StatusOrders
    .FirstOrDefaultAsync(s => s.StatusName == "Cancelled");

if (cancelledStatus != null) // Check StatusOrder exists
{
    // Safe to use cancelledStatus.StatusId
}
```

---

## 📝 Controller Actions

### ApproveRefund (New Action)
```csharp
[HttpPost("ApproveRefund")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveRefund(long refundId)
{
    int staffId = GetCurrentStaffId();
    await _refundService.ApproveOrUpdateStatusAsync(refundId, "Approved", staffId);
    
    // Automatically updates:
    // - OrderRefund.RefundStatus = "Approved"
    // - OrderRefund.ApprovedBy = staffId
    // - OrderRefund.ApprovedAt = DateTime.Now
    // - Order.RefundStatus = "Approved" ✅
    
    TempData["SuccessMessage"] = "✅ Đã duyệt yêu cầu hoàn tiền.";
    return RedirectToAction(nameof(Manage));
}
```

### RejectRefund (New Action)
```csharp
[HttpPost("RejectRefund")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RejectRefund(long refundId)
{
    int staffId = GetCurrentStaffId();
    await _refundService.ApproveOrUpdateStatusAsync(refundId, "Rejected", staffId);
    
    // Automatically updates:
    // - OrderRefund.RefundStatus = "Rejected"
    // - OrderRefund.ApprovedBy = staffId
    // - OrderRefund.ApprovedAt = DateTime.Now
    // - Order.RefundStatus = "Rejected" ✅
    
    TempData["SuccessMessage"] = "❌ Đã từ chối yêu cầu hoàn tiền.";
    return RedirectToAction(nameof(Manage));
}
```

### UpdateStatus (For Processing/Completed)
```csharp
[HttpPost("UpdateStatus")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateStatus(long refundId, string newStatus)
{
    int staffId = GetCurrentStaffId();
    await _refundService.ApproveOrUpdateStatusAsync(refundId, newStatus, staffId);
    
    // If newStatus = "Processing":
    // - Order.RefundStatus = "Processing" ✅
    
    // If newStatus = "Refunded":
    // - Order.StatusId = Cancelled ✅
    // - Order.RefundStatus = "Refunded" ✅
    // - OrderStatusHistory created ✅
    
    TempData["SuccessMessage"] = GetSuccessMessageByStatus(newStatus);
    return RedirectToAction(nameof(Manage));
}
```

---

## 🎯 Benefits

### 1. **Data Consistency**
- ✅ Order và OrderRefund luôn đồng bộ trạng thái
- ✅ Không có trường hợp Order vẫn "Active" nhưng refund đã "Completed"

### 2. **Audit Trail**
- ✅ OrderStatusHistory ghi lại đầy đủ lịch sử thay đổi
- ✅ Biết ai, khi nào, vì sao Order bị Cancelled

### 3. **Business Logic Clarity**
- ✅ Hoàn tiền thành công → Order tự động Cancelled
- ✅ Staff không cần thủ công update 2 bảng

### 4. **Query Efficiency**
```csharp
// Query all refunded orders
var refundedOrders = await _context.Orders
    .Where(o => o.RefundStatus == "Refunded" && o.StatusId == cancelledStatusId)
    .ToListAsync();

// Query pending refunds
var pendingRefunds = await _context.Orders
    .Where(o => o.RefundStatus == "Pending")
    .Include(o => o.OrderRefund)
    .ToListAsync();
```

---

## 🚨 Edge Cases

### Case 1: StatusOrder "Cancelled" not found
```csharp
var cancelledStatus = await _context.StatusOrders
    .FirstOrDefaultAsync(s => s.StatusName == "Cancelled");

if (cancelledStatus == null)
{
    // Log error or create default Cancelled status
    throw new InvalidOperationException("StatusOrder 'Cancelled' not found in database. Please seed data.");
}
```

**Fix:** Ensure StatusOrder table has "Cancelled" record:
```sql
INSERT INTO StatusOrder (StatusName, Description, CreatedAt)
VALUES ('Cancelled', 'Đơn hàng đã bị hủy', GETDATE());
```

### Case 2: Order already Cancelled
```csharp
if (refund.Order.StatusId == cancelledStatusId)
{
    // Order already cancelled, skip status update but still mark refund as Refunded
    refund.Order.RefundStatus = "Refunded";
    // Do not create duplicate StatusHistory
}
```

### Case 3: Concurrent Updates
- Use database transactions (implicit in EF Core SaveChangesAsync)
- Add optimistic concurrency with `RowVersion` if needed

---

## 📚 Related Files

| File | Purpose |
|------|---------|
| `DAL/Repositories/OrderRefundRepository.cs` | ✅ **Modified** - Sync Order status |
| `BLL/Services/OrderRefundService.cs` | State machine validation |
| `ASP_LorKingDom/Controllers/OrderRefundAdminController.cs` | ✅ **Modified** - ApproveRefund, RejectRefund actions |
| `DAL/Models/Order.cs` | Order entity with StatusId & RefundStatus |
| `DAL/Models/OrderRefund.cs` | OrderRefund entity |
| `DAL/Models/OrderStatusHistory.cs` | Audit trail entity |
| `DAL/Models/StatusOrder.cs` | Lookup table for order statuses |

---

## ✅ Checklist

- [x] Update `OrderRefundRepository.UpdateStatusAsync()` to sync Order
- [x] Handle "Approved" → Update `Order.RefundStatus`
- [x] Handle "Rejected" → Update `Order.RefundStatus`
- [x] Handle "Processing" → Update `Order.RefundStatus`
- [x] Handle "Refunded" → Update `Order.StatusId` to Cancelled
- [x] Handle "Refunded" → Create `OrderStatusHistory` record
- [x] Add null safety checks for `refund.Order`
- [x] Add null safety checks for `cancelledStatus`
- [x] Add `Processing` to valid statuses list
- [x] Include `Order` navigation property in query
- [x] Documentation complete

---

**Last Updated:** October 29, 2025  
**Status:** ✅ Implemented & Documented  
**Impact:** Order và OrderRefund đã được đồng bộ hóa tự động
