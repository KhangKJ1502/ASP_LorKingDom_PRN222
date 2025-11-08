# State Charts Documentation

## 📋 Tổng quan
Thư mục này chứa tất cả các sơ đồ trạng thái (State Charts) của các entities quan trọng trong hệ thống **ASP_LorKingDom_PRN222**. Mỗi sơ đồ mô tả vòng đời, các trạng thái, transitions, và business rules của từng entity.

---

## 📊 Danh sách State Charts

### 1. [Order Status State Chart](./01_OrderStatusStatechart.md)
**Entity**: `Order` + `OrderStatusHistory`  
**Status Field**: `StatusId` (FK to `StatusOrder`)  
**States**: 
- Pending → Confirmed → Processing → Shipping → Delivered → Completed
- Cancel paths: Pending/Confirmed → Canceled
- Return path: Completed → Returned

**Key Features**:
- Order lifecycle từ tạo đơn đến hoàn thành
- Xử lý hủy đơn và hoàn trả
- Lưu lịch sử thay đổi trạng thái trong `OrderStatusHistory`

---

### 2. [Order Refund State Chart](./02_OrderRefundStatechart.md)
**Entity**: `OrderRefund`  
**Status Field**: `RefundStatus`  
**States**: 
- Requested → Approved → Processing → Completed
- Rejected path từ Requested/Approved
- Canceled path từ Requested

**Key Features**:
- Quy trình hoàn tiền cho đơn hàng
- Admin duyệt/từ chối yêu cầu
- Xử lý tiền hoàn về wallet

---

### 3. [Account Status State Chart](./03_AccountStatusStatechart.md)
**Entity**: `Account`  
**Status Field**: `Status`  
**States**: 
- Pending → Active → (Suspended/Locked/Deleted)
- Reactivation paths

**Key Features**:
- Quản lý trạng thái tài khoản người dùng
- Xử lý kích hoạt, khóa, xóa tài khoản
- Email verification và security

---

### 4. [Product Status State Chart](./04_ProductStatusStatechart.md)
**Entity**: `Product`  
**Status Field**: `ProductStatus`  
**States**: 
- Draft → Active → (Inactive/OutOfStock/Discontinued)

**Key Features**:
- Lifecycle sản phẩm từ tạo đến ngừng bán
- Quản lý tồn kho và trạng thái hiển thị
- Soft delete với IsDeleted flag

---

### 5. [Notification State Chart](./05_NotificationStatechart.md)
**Entity**: `Notification` + `UserNotification`  
**Status Fields**: `IsSent`, `IsCanceled` (Notification) + `IsRead` (UserNotification)  
**States**: 
- Draft → Scheduled → Sent → Delivered → Read
- Cancel/Expire paths

**Key Features**:
- Notification lifecycle với worker service
- User-specific delivery tracking
- Read/Unread management

---

### 6. [Chat Conversation State Chart](./06_ChatConversationStatechart.md)
**Entity**: `ChatMessage` + `OnlineStaff`  
**Status**: Implicit (based on timestamps and online status)  
**States**: 
- Waiting → Assigned → Active → Resolved → Closed

**Key Features**:
- Customer support chat lifecycle
- Staff assignment và availability
- Real-time updates với SignalR

---

### 7. [Voucher State Chart](./07_VoucherStatechart.md) ✨ NEW
**Entity**: `Voucher`  
**Status Field**: `Status`  
**States**: 
- Draft → Active → (Expired/Inactive)

**Key Features**:
- Voucher lifecycle với time-based expiration
- Usage limit tracking
- Stackable logic

---

### 8. [Wallet State Chart](./08_WalletStatechart.md) ✨ NEW
**Entity**: `Wallet`  
**Status Field**: `Status`  
**States**: 
- Active → (Frozen/Closed)

**Key Features**:
- Wallet lifecycle và balance management
- Security freeze/unfreeze
- Multi-currency support

---

### 9. [Wallet Transaction State Chart](./09_WalletTransactionStatechart.md) ✨ NEW
**Entity**: `WalletTransaction`  
**Status Field**: `Status`  
**States**: 
- Pending → Completed/Failed/Canceled

**Key Features**:
- Transaction lifecycle với idempotency
- Balance tracking (before/after)
- Integration với Payment & Refund

---

### 10. [Promotion State Chart](./10_PromotionStatechart.md) ✨ NEW
**Entity**: `Promotion`  
**Status Field**: `Status`  
**States**: 
- Draft → Active → (Inactive/Expired)

**Key Features**:
- Promotion campaign lifecycle
- Time-based activation
- Product association

---

### 11. [Payment History State Chart](./11_PaymentHistoryStatechart.md) ✨ NEW
**Entity**: `PaymentHistory`  
**Status Field**: `PaymentStatus`  
**States**: 
- Pending → Success/Failed/Refunded

**Key Features**:
- Payment transaction tracking
- Integration với Order & Wallet
- External payment gateway handling

---

## 🎯 Tổng hợp Entities có State

| Entity | Status Field | States Count | Complexity | Priority |
|--------|-------------|--------------|------------|----------|
| Order | StatusId (FK) | 8+ | ⭐⭐⭐⭐⭐ High | Critical |
| OrderRefund | RefundStatus | 6 | ⭐⭐⭐⭐ Medium-High | High |
| Account | Status | 6 | ⭐⭐⭐⭐ Medium-High | Critical |
| Product | ProductStatus | 5 | ⭐⭐⭐ Medium | High |
| Notification | IsSent/IsCanceled | 8 | ⭐⭐⭐⭐ Medium-High | Medium |
| ChatMessage | Implicit | 5 | ⭐⭐⭐ Medium | Medium |
| Voucher | Status | 3 | ⭐⭐ Low-Medium | Medium |
| Wallet | Status | 3 | ⭐⭐⭐ Medium | High |
| WalletTransaction | Status | 4 | ⭐⭐⭐⭐ Medium-High | High |
| Promotion | Status | 3 | ⭐⭐ Low-Medium | Medium |
| PaymentHistory | PaymentStatus | 4 | ⭐⭐⭐⭐ Medium-High | Critical |

---

## 🔄 State Dependencies

### Order Flow
```
Order (StatusId) 
  ↓ triggers
OrderStatusHistory (history tracking)
  ↓ may trigger
PaymentHistory (payment processing)
  ↓ may trigger
WalletTransaction (if wallet payment)
  ↓ updates
Wallet (balance update)
```

### Refund Flow
```
OrderRefund (RefundStatus)
  ↓ creates
WalletTransaction (refund txn)
  ↓ updates
Wallet (refund balance)
  ↓ references
Order (RefundStatus update)
```

### Product-Promotion Flow
```
Promotion (Status: Active/Inactive)
  ↓ applies to
Product (with DiscountPercent)
  ↓ affects
Order (final price calculation)
```

### Notification Flow
```
Notification (IsSent/IsCanceled)
  ↓ creates
UserNotification (IsRead)
  ↓ displays in
UI (badge, offcanvas, index)
```

---

## 📐 Design Patterns Used

### 1. **State Pattern**
- Explicit state tracking với status fields
- State transitions với validation
- State history tracking (OrderStatusHistory)

### 2. **Observer Pattern**
- Status changes trigger events
- Worker services monitor state changes
- Notifications on state transitions

### 3. **Strategy Pattern**
- Different behaviors per state
- State-specific validation rules
- State-specific UI rendering

### 4. **Command Pattern**
- State transitions as commands
- Idempotent operations (WalletTransaction)
- Transaction rollback support

---

## 🛠️ Implementation Guidelines

### State Transition Best Practices

1. **Validation First**
   ```csharp
   if (!CanTransitionTo(newStatus))
       throw new InvalidOperationException("Invalid state transition");
   ```

2. **Atomic Updates**
   ```csharp
   using var transaction = await _context.Database.BeginTransactionAsync();
   // Update entity status
   // Log history
   // Trigger side effects
   await transaction.CommitAsync();
   ```

3. **History Tracking**
   ```csharp
   await _historyRepo.AddAsync(new StatusHistory {
       EntityId = entity.Id,
       OldStatus = oldStatus,
       NewStatus = newStatus,
       ChangedBy = userId,
       ChangedAt = DateTime.UtcNow
   });
   ```

4. **Event Notifications**
   ```csharp
   await _notificationService.NotifyStatusChange(entity, oldStatus, newStatus);
   ```

### State Query Patterns

```csharp
// Filter by status
var activeItems = await _repo.GetByStatusAsync("Active");

// Check state validity
var canCancel = order.StatusId <= StatusIds.Confirmed;

// Get state history
var history = await _historyRepo.GetByEntityIdAsync(orderId);
```

---

## 📝 Documentation Format

Mỗi State Chart document bao gồm:

1. **Mermaid State Diagram** - Visual representation
2. **State Descriptions** - Chi tiết từng trạng thái
3. **Transition Matrix** - Bảng chuyển đổi trạng thái
4. **Key Methods** - Các methods quan trọng
5. **Edge Cases** - Xử lý trường hợp đặc biệt
6. **Logging Strategy** - Chiến lược ghi log
7. **UI Flow** - Luồng giao diện người dùng
8. **Sequence Diagrams** - Luồng xử lý chi tiết

---

## 🚀 Usage Examples

### Example 1: Order Status Transition
```csharp
// Check current state
if (order.StatusId == StatusIds.Pending)
{
    // Transition to Confirmed
    await _orderService.UpdateStatusAsync(orderId, StatusIds.Confirmed, adminId);
    // Triggers: OrderStatusHistory log, Notification to customer
}
```

### Example 2: Wallet Transaction
```csharp
// Create pending transaction
var txn = await _walletService.CreateTransactionAsync(new WalletTransactionDto {
    WalletId = walletId,
    Amount = 100000,
    TxnType = "Deposit",
    Status = "Pending"
});

// Complete transaction
await _walletService.CompleteTransactionAsync(txn.WalletTransactionId);
// Updates: Status → Completed, Wallet.Balance updated, CompletedAt set
```

### Example 3: Voucher Activation
```csharp
// Auto-activate when StartDate reached
var voucher = await _voucherService.GetByIdAsync(voucherId);
if (voucher.Status == "Draft" && voucher.StartDate <= DateTime.UtcNow)
{
    await _voucherService.ActivateAsync(voucherId);
    // Status: Draft → Active
}
```

---

## 🔍 Testing State Machines

### Unit Tests
```csharp
[Fact]
public async Task CanTransitionFromPendingToConfirmed()
{
    // Arrange
    var order = new Order { StatusId = StatusIds.Pending };
    
    // Act
    var result = await _orderService.CanTransitionAsync(order, StatusIds.Confirmed);
    
    // Assert
    Assert.True(result);
}
```

### Integration Tests
```csharp
[Fact]
public async Task OrderCompleteFlow_CreatesHistoryAndNotification()
{
    // Full lifecycle test
    var orderId = await CreateTestOrder();
    await ConfirmOrder(orderId);
    await ProcessOrder(orderId);
    await ShipOrder(orderId);
    await DeliverOrder(orderId);
    
    var history = await _historyRepo.GetByOrderIdAsync(orderId);
    Assert.Equal(5, history.Count); // 5 status changes
}
```

---

## 📚 Related Documentation

- [Architecture Overview](../Architecture/)
- [Database Schema](../Database/)
- [API Documentation](../API/)
- [Business Rules](../BusinessRules/)

---

## 🎯 Future Enhancements

1. **State Machine Validation Library**
   - Centralized state transition validation
   - Declarative state rules

2. **State Change Events**
   - Domain events for state changes
   - Event sourcing for audit trail

3. **State Visualization Dashboard**
   - Real-time state distribution charts
   - Transition frequency analytics

4. **State Recovery Mechanisms**
   - Auto-retry for failed states
   - Manual state override with approval

---

**Last Updated**: 2025-11-05  
**Maintained By**: Development Team  
**Version**: 1.0
