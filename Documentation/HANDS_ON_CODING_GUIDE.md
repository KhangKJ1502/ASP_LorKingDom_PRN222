# 🎯 HANDS-ON CODING GUIDE - SignalR & Workers
## Từ Zero đến Pro - Sửa Code Từng Bước

---

## 📚 MỤC LỤC
1. [Bài 1: Hiểu ChatHub Code](#bài-1-hiểu-chathub-code)
2. [Bài 2: Fix ChatHub Issues](#bài-2-fix-chathub-issues)
3. [Bài 3: Hiểu Worker Code](#bài-3-hiểu-worker-code)
4. [Bài 4: Fix Worker Issues](#bài-4-fix-worker-issues)
5. [Bài 5: Thêm Feature Mới](#bài-5-thêm-feature-mới)
6. [Bài 6: Integration & Testing](#bài-6-integration--testing)

---

# 🎓 BÀI 1: HIỂU CHATHUB CODE

## 1.1 Cấu Trúc Cơ Bản của ChatHub

**File**: `WebUI/ChatHubs/ChatHub.cs`

### **Tìm hiểu: Các phần chính**

```csharp
public class ChatHub : Hub  // ← Kế thừa từ Hub
{
    private readonly IChatService _chatService;  // ← Service
    private readonly ILogger<ChatHub> _logger;   // ← Logging
    
    // ← In-memory storage (concurrent - thread-safe)
    private static readonly ConcurrentDictionary<string, int> _staffConnCount;
    private static readonly ConcurrentDictionary<string, string> _staffNames;
    private static readonly ConcurrentDictionary<string, bool> _staffPageState;
}
```

### ✅ **EXERCISE 1.1**: Read ChatHub.cs và trả lời câu hỏi

**TODO: Mở file `ChatHub.cs` và trả lời:**

1. **ConcurrentDictionary là gì?**
   - [ ] A. Giống List nhưng chậm hơn
   - [ ] B. Thread-safe dictionary (không cần lock)
   - [ ] C. Dictionary bình thường nhưng tên dài
   - **Đáp án**: B ✅

2. **Tại sao cần in-memory storage (_staffConnCount)?**
   - [ ] A. Để lưu vào database
   - [ ] B. Để tracking số connection của staff
   - [ ] C. Để hiển thị trong UI
   - **Đáp án**: B ✅

3. **Tại sao dùng `static` cho in-memory storage?**
   - [ ] A. Để truy cập nhanh hơn
   - [ ] B. Để chia sẻ giữa tất cả connections
   - [ ] C. Để tiết kiệm memory
   - **Đáp án**: B ✅
   - **Giải thích**: Static → toàn bộ connections dùng chung 1 dictionary

---

## 1.2 OnConnectedAsync - Connection Lifecycle

**Code gốc:**
```csharp
public override async Task OnConnectedAsync()
{
    try
    {
        // 1️⃣ Lấy query parameters từ URL
        var http = Context.GetHttpContext();
        var userId = Q(http, "userId");        
        var isStaff = QBool(http, "isStaff");  
        var name = Q(http, "name");             

        // 2️⃣ Validate
        if (string.IsNullOrWhiteSpace(userId))
        {
            Context.Abort(); // ← Từ chối kết nối
            return;
        }

        // 3️⃣ Save to database
        await _chatService.UserConnectedAsync(
            userId, isStaff, name, Context.ConnectionId);

        // 4️⃣ Add to group
        await Groups.AddToGroupAsync(
            Context.ConnectionId, $"user:{userId}");

        // 5️⃣ If staff
        if (isStaff)
        {
            _staffNames[userId] = name;
            _staffConnCount.AddOrUpdate(userId, 1, (_, n) => n + 1);
            
            // Broadcast online
            if (_staffConnCount[userId] == 1)
            {
                await Clients.All.SendAsync("staffPresence", new
                {
                    staffId = userId,
                    online = true
                });
            }
        }

        // 6️⃣ Broadcast presence
        await BroadcastPresence(userId, true);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in OnConnectedAsync");
        throw;
    }

    await base.OnConnectedAsync();
}
```

### ✅ **EXERCISE 1.2**: Hiểu Flow

**Hãy vẽ sơ đồ flow:**

```
Browser (Customer)
    │
    └─ WebSocket: /chatHub?userId=123&isStaff=false&name=John
           │
           ▼ (kết nối đến server)
        
    OnConnectedAsync()
    ├─ Lấy userId="123", isStaff=false, name="John"
    ├─ Validate userId (không null)
    ├─ Save DB: UserConnection { userId, name, connectionId }
    ├─ AddToGroupAsync("user:123")  ← ⭐ Quan trọng!
    ├─ isStaff=false? → không vào if block
    ├─ BroadcastPresence(userId, true)
    │  ├─ Gửi đến ALL: "userOnline" event
    │  └─ Client khác sẽ nhận được
    │
    └─ await base.OnConnectedAsync()

Frontend JavaScript:
    │
    └─ connection.on("userOnline", (data) => {...})
        └─ Update UI: hiển thị "John is online"
```

### 🔍 **EXERCISE 1.3**: Hiểu Groups

**Groups có thể tưởng tượng như:**
- **🏠 Group "user:123"** → Chỉ có connection của user 123
  - Khi gửi: `Clients.Group("user:123").SendAsync(...)` → chỉ user 123 nhận
  
- **🏬 Group "all_staff"** → Tất cả staff connections
  - Khi gửi: `Clients.Group("all_staff").SendAsync(...)` → tất cả staff nhận

- **🌍 Clients.All** → Mọi người connected
  - Khi gửi: `Clients.All.SendAsync(...)` → toàn bộ nhận

**Hãy trả lời:**

```
Q: Nếu user A muốn gửi tin nhắn cho user B, cần code gì?
A: await Clients.Group($"user:{userBId}").SendAsync("receiveMessage", message);

Q: Tại sao không dùng Clients.All?
A: Vì toàn bộ users sẽ nhận (lãng phí bandwidth + privacy)

Q: User A có thể nhận tin nhắn được gửi cho Group "user:B" không?
A: Không, vì A không nằm trong Group "user:B"
```

---

## 1.3 Điểm Yếu & Lỗi Thường Gặp

### ⚠️ **Vấn đề 1: Static Dictionary Mất Dữ Liệu Khi Restart App**

```csharp
// ❌ HIỆN TẠI (lỗi)
private static readonly ConcurrentDictionary<string, int> _staffConnCount;

// 👉 Khi app restart:
//    - Dictionary bị reset → mất dữ liệu
//    - Staff count về 0
//    - UI không sync

// ✅ CẢI THIỆN
// Lưu vào database thay vì in-memory
await _chatService.UpdateStaffOnlineStatusAsync(userId, isOnline);
```

### ⚠️ **Vấn đề 2: Không Handle Disconnect**

```csharp
// ❌ Nếu OnDisconnectedAsync không hoạt động
// → Staff vẫn hiển thị online dù đã disconnect
// → Memory leak: ConcurrentDictionary không được clear

// ✅ Fix: Đảm bảo OnDisconnectedAsync được call
public override async Task OnDisconnectedAsync(Exception? exception)
{
    var userId = Q(Context.GetHttpContext(), "userId");
    var isStaff = QBool(Context.GetHttpContext(), "isStaff");
    
    if (isStaff && _staffConnCount.TryGetValue(userId, out var count))
    {
        _staffConnCount.AddOrUpdate(userId, 0, (_, _) => count - 1);
        
        if (count == 1)
        {
            await Clients.All.SendAsync("staffPresence", new
            {
                staffId = userId,
                online = false
            });
        }
    }
    
    await base.OnDisconnectedAsync(exception);
}
```

### ⚠️ **Vấn đề 3: Không Validate Input Từ Client**

```csharp
// ❌ BẮC VÀO (lỗi)
var userId = Q(http, "userId"); // Không validate
if (userId == "admin") // Attacker có thể giả làm admin!
{
    // grant admin access
}

// ✅ ĐÚNG
var userId = Q(http, "userId");
if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out _))
{
    Context.Abort(); // Reject connection
    return;
}

// Validate backend
var user = await _userService.GetAsync(userId);
if (user == null || !user.IsActive)
{
    Context.Abort();
    return;
}
```

---

# 🔧 BÀI 2: FIX CHATHUB ISSUES

## 2.1 ISSUE: Message Không Gửi Được (Tìm & Sửa)

### ❌ **PROBLEM**: Dòng lệnh sai trong SendMessage()

**Hiện tại (sai):**
```csharp
public async Task SendMessage(SendMessageDto dto)
{
    var message = await _chatService.SaveMessageAsync(dto);
    
    // ❌ WRONG: Gửi đến group "user:sender" (chính user gửi)
    await Clients.Group($"user:{dto.SenderId}")
        .SendAsync("receiveMessage", message);
}

// Result: Chỉ người gửi nhận, người nhận không nhận!
```

### ✅ **SOLUTION**: Gửi đến receiver

**File**: `ChatHub.cs` - tìm method `SendMessage()`

**Trước:**
```csharp
public async Task SendMessage(SendMessageDto dto)
{
    var message = await _chatService.SaveMessageAsync(dto);
    
    await Clients.Group($"user:{dto.SenderId}")  // ← SAI
        .SendAsync("receiveMessage", message);
}
```

**Sau:**
```csharp
public async Task SendMessage(SendMessageDto dto)
{
    var message = await _chatService.SaveMessageAsync(dto);
    
    // ✅ Gửi đến receiver
    await Clients.Group($"user:{dto.RecipientId}")
        .SendAsync("receiveMessage", message);
    
    // ✅ Gửi confirmation cho sender
    await Clients.Caller
        .SendAsync("messageSent", message.Id);
}
```

### 🎯 **EXERCISE 2.1**: Thực hành sửa

```
1. Mở ChatHub.cs
2. Tìm method SendMessage()
3. Thay đổi:
   - Từ: Clients.Group($"user:{dto.SenderId}")
   - Sang: Clients.Group($"user:{dto.RecipientId}")
4. Thêm dòng gửi confirmation
5. Save file
```

---

## 2.2 ISSUE: Staff Presence Not Updating

### ❌ **PROBLEM**: Staff offline nhưng vẫn hiển thị online

**Nguyên nhân:**
```csharp
// ❌ Khi staff disconnect, code không được gọi
public override async Task OnDisconnectedAsync(Exception? exception)
{
    // ... code bị thiếu hoặc lỗi
    // Staff info không được xóa khỏi _staffConnCount
}
```

### ✅ **SOLUTION**: Hoàn thiện OnDisconnectedAsync

**File**: `ChatHub.cs` - tìm method `OnDisconnectedAsync()`

**Code cần thêm:**
```csharp
public override async Task OnDisconnectedAsync(Exception? exception)
{
    try
    {
        // 1. Lấy userId
        var http = Context.GetHttpContext();
        var userId = Q(http, "userId");
        var isStaff = QBool(http, "isStaff");
        
        // 2. Update database
        await _chatService.UserDisconnectedAsync(userId, Context.ConnectionId);
        
        // 3. Nếu là staff
        if (isStaff)
        {
            // ✅ Giảm connection count
            if (_staffConnCount.TryGetValue(userId, out var count))
            {
                if (count > 1)
                {
                    _staffConnCount[userId] = count - 1;
                }
                else
                {
                    // ✅ Xóa khỏi dictionary
                    _staffConnCount.TryRemove(userId, out _);
                    _staffNames.TryRemove(userId, out _);
                    _staffPageState.TryRemove(userId, out _);
                    
                    // ✅ Broadcast offline
                    await Clients.All.SendAsync("staffPresence", new
                    {
                        staffId = userId,
                        online = false
                    });
                }
            }
        }
        
        // 4. Broadcast presence
        await BroadcastPresence(userId, false);
        
        _logger.LogInformation("User {UserId} disconnected", userId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in OnDisconnectedAsync");
    }
    
    await base.OnDisconnectedAsync(exception);
}
```

### 🎯 **EXERCISE 2.2**: Thực hành sửa

```
1. Mở ChatHub.cs
2. Tìm method OnDisconnectedAsync()
3. Kiểm tra có code xóa staff khỏi _staffConnCount không
4. Nếu không có, thêm code ở trên
5. Save & Test
```

---

## 2.3 ISSUE: Startup Crash - CORS Error

### ❌ **PROBLEM**: SignalR connection fails (CORS error)

**Browser Console Error:**
```
WebSocket connection failed: 
Access to XMLHttpRequest has been blocked by CORS policy
```

### ✅ **SOLUTION**: Cấu hình CORS trong Program.cs

**File**: `Program.cs`

**Trước (sai hoặc thiếu):**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("https://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              // ❌ THIẾU: AllowCredentials()
    });
});
```

**Sau (đúng):**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins("https://localhost", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // ✅ QUAN TRỌNG: SignalR cần credentials
    });
});

// Áp dụng CORS
app.UseCors("AllowSignalR");

// Đặt TRƯỚC MapHub
app.MapHub<ChatHub>("/chatHub");
```

### 🎯 **EXERCISE 2.3**: Thực hành sửa

```
1. Mở Program.cs
2. Tìm AddCors()
3. Thêm .AllowCredentials()
4. Đảm bảo UseCors() được gọi trước MapHub()
5. Run app & test connection
```

---

## 2.4 ISSUE: Performance - Too Many Messages

### ❌ **PROBLEM**: Gửi quá nhiều message → lag

```csharp
// ❌ Gửi từng message 1 lần (inefficient)
public async Task BroadcastPresence(string userId, bool isOnline)
{
    await Clients.All.SendAsync("userOnline", userId, isOnline);
    // Gọi liên tục khi có nhiều user
}
```

### ✅ **SOLUTION**: Batch messages & Compression

**File**: `Program.cs`

```csharp
// 1️⃣ Add compression
builder.Services.AddSignalR(options =>
{
    // Giới hạn kích thước message
    options.MaximumReceiveMessageSize = 1024 * 64; // 64KB
    
    // Buffer capacity cho streaming
    options.StreamBufferCapacity = 32;
});

// 2️⃣ Add MessagePack protocol (binary - nhỏ hơn JSON)
builder.Services.AddSignalR()
    .AddMessagePackProtocol();
```

**Trong ChatHub:**
```csharp
// Batch broadcast many users
public async Task BroadcastPresenceMany(List<PresenceData> presenceList)
{
    // Gửi 1 lần thay vì N lần
    await Clients.All.SendAsync("presenceUpdate", presenceList);
}
```

---

# 🔄 BÀI 3: HIỂU WORKER CODE

## 3.1 NotificationWorkerService - Cấu Trúc

**File**: `WebUI/Workers/NotificationWorkerService.cs`

### **Cấu Trúc Cơ Bản**

```csharp
public class NotificationWorkerService : BackgroundService
{
    private readonly ILogger<NotificationWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    // ⏱️ Chạy mỗi 1 phút
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    
    // 🔒 Semaphore: chỉ cho 1 execution tại 1 lúc
    private readonly SemaphoreSlim _gate = new(1, 1);
}
```

### ✅ **EXERCISE 3.1**: Hiểu Semaphore

**Semaphore là gì?**

```
Tưởng tượng: Người gọi cửa hàng

🏪 Cửa hàng (SemaphoreSlim(1, 1))
   ├─ maxCount = 1 (1 người vào)
   └─ currentCount = 1 (hiện có slot trống)

Người A gọi → Nhân viên bận
   ├─ WaitAsync(0) → Không có slot trống
   ├─ Return False
   └─ A bỏ qua (skip execution)

Người B gọi (khác) → Nhân viên rảnh
   ├─ WaitAsync(0) → Có slot trống
   ├─ Return True
   ├─ currentCount = 0 (hết slot)
   └─ B vào được

Khi B xong → Release()
   ├─ currentCount = 1 (có slot trống)
   └─ Người tiếp theo có thể vào
```

**Tại sao cần?**
```
❌ Nếu KHÔNG có Semaphore:
Lúc 14:00:00 - Tick 1 start
   ├─ Query notifications (1 phút)
   ├─ Create records (30s)
   └─ (chưa xong)

Lúc 14:01:00 - Tick 2 start (execution 1 chưa xong!)
   ├─ Query notifications again (bị duplicate!)
   ├─ Create records again
   └─ Database error: unique constraint violation

Result: 💥 DATA CORRUPTION

✅ Nếu CÓ Semaphore:
Lúc 14:00:00 - Tick 1 start
   ├─ WaitAsync() → success
   ├─ Execute...

Lúc 14:01:00 - Tick 2 start
   ├─ WaitAsync() → FAIL (Tick 1 chưa xong)
   ├─ Log warning: "Previous execution still running"
   └─ Skip this tick

Result: ✅ NO CORRUPTION
```

### 🎯 **EXERCISE 3.2**: Hiểu ExecuteAsync() Flow

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // 1️⃣ Log start
    _logger.LogInformation("NotificationWorkerService starting.");
    
    // 2️⃣ Random delay (0-5000ms) - tránh spike
    var startupJitterMs = Random.Shared.Next(0, 5000);
    try
    {
        await Task.Delay(startupJitterMs, stoppingToken);
    }
    catch (OperationCanceledException)
    {
        return; // App shutdown during startup
    }
    
    // 3️⃣ Tạo timer (1 phút)
    var timer = new PeriodicTimer(_interval);
    
    try
    {
        // 4️⃣ LOOP: chạy mỗi tick
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // 5️⃣ Gọi business logic (safe execution)
            await SafeProcessOnceAsync(stoppingToken);
        }
    }
    catch (OperationCanceledException) { } // Expected
    catch (Exception ex)
    {
        _logger.LogError(ex, "NotificationWorkerService crashed.");
    }
    finally
    {
        // 6️⃣ Cleanup
        timer.Dispose();
        _logger.LogInformation("NotificationWorkerService stopping.");
    }
}
```

**Flow diagram:**
```
App Start
    │
    └─ ExecuteAsync(stoppingToken)
       │
       ├─ Wait 0-5000ms (jitter)
       │
       ├─ Create PeriodicTimer(1 min)
       │
       ├─ LOOP:
       │  ├─ [Tick 1] WaitForNextTickAsync() → True
       │  │  └─ SafeProcessOnceAsync()
       │  │     ├─ WaitAsync(_gate) → success
       │  │     ├─ ProcessDueNotificationsAsync()
       │  │     └─ Release(_gate)
       │  │
       │  ├─ [Tick 2 (1 min later)] WaitForNextTickAsync() → True
       │  │  └─ SafeProcessOnceAsync()
       │  │
       │  └─ [App Shutdown] WaitForNextTickAsync() → False
       │     └─ Exit loop
       │
       └─ Dispose timer

Done
```

---

## 3.2 SafeProcessOnceAsync & ProcessDueNotificationsAsync

### **Phân tích từng bước:**

```csharp
private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    // 1️⃣ Kiểm tra semaphore (non-blocking)
    if (!await _gate.WaitAsync(0, ct))
    {
        // Nếu không thể acquire semaphore trong 0ms
        _logger.LogWarning("Previous execution still running; skipping this tick.");
        return; // Skip
    }
    
    // Nếu đến đây → acquired semaphore ✅
    
    var sw = Stopwatch.StartNew(); // ⏱️ Measure time
    try
    {
        // 2️⃣ Gọi business logic
        await ProcessDueNotificationsAsync(ct);
    }
    catch (OperationCanceledException) { }  // Expected when app shutdown
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in processing due notifications.");
    }
    finally
    {
        sw.Stop();
        _gate.Release(); // 3️⃣ ⭐ QUAN TRỌNG: Release semaphore
        _logger.LogDebug("Dispatch run finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
    }
}

private async Task ProcessDueNotificationsAsync(CancellationToken ct)
{
    // 1️⃣ Tạo scope (vì INotificationService là Scoped)
    await using var scope = _serviceProvider.CreateAsyncScope();
    
    // 2️⃣ Get service từ scope
    var notificationService = scope.ServiceProvider
        .GetRequiredService<INotificationService>();
    
    // 3️⃣ Gọi service method
    var count = await notificationService.DispatchDueAsync();
    
    // 4️⃣ Log result
    if (count > 0)
    {
        _logger.LogInformation(
            "Processed {Count} due notification(s) at {UtcTime}.",
            count,
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
```

### ✅ **EXERCISE 3.3**: Trả lời câu hỏi

```
Q1: Tại sao cần CreateAsyncScope()?
A:  Vì INotificationService là Scoped service
    Scoped chỉ có thể inject vào Controllers
    Trong Singleton Worker, cần tạo scope mới cho mỗi execution
    
Q2: Nếu ProcessDueNotificationsAsync() bị lỗi, timer có bị crash?
A:  Không, vì có try-catch
    Lỗi sẽ được log
    Timer tiếp tục chạy tick tiếp theo
    
Q3: _gate.Release() có cần thiết không?
A:  CÓ, rất cần!
    Nếu không Release(), semaphore sẽ bị "stuck"
    Những tick sau sẽ skip vĩnh viễn
    Do đó cần đặt trong finally{}
    
Q4: Nếu ProcessDueNotifications() mất 2 phút, sao?
A:  SafeProcessOnceAsync() sẽ:
    - Tick 1 (0-1 phút): start execution
    - Tick 2 (1-2 phút): WaitAsync() fail, skip (execution 1 chưa xong)
    - Tick 3 (2-3 phút): WaitAsync() success, execution 2 start
    
    Điều này là bình thường và mong muốn!
```

---

# 🔧 BÀI 4: FIX WORKER ISSUES

## 4.1 ISSUE: Worker Never Executes

### ❌ **PROBLEM**: NotificationWorkerService không chạy

**Triệu chứng:**
```
- App chạy bình thường
- Log không hiển thị: "NotificationWorkerService starting"
- Notification không được gửi
```

### ✅ **SOLUTION**: Kiểm tra Registration

**File**: `Program.cs`

**Kiểm tra danh sách:**

```csharp
// ✅ Phải có dòng này
builder.Services.AddHostedService<NotificationWorkerService>();

// ✅ Hoặc nếu dùng full namespace
builder.Services.AddHostedService<WebUI.Workers.NotificationWorkerService>();
```

**Debug tip:**
```csharp
// Thêm vào Program.cs để verify
var descriptor = builder.Services.FirstOrDefault(d => 
    d.ServiceType == typeof(IHostedService) && 
    d.ImplementationType?.Name == "NotificationWorkerService");

if (descriptor == null)
{
    Console.WriteLine("❌ NotificationWorkerService NOT registered!");
}
else
{
    Console.WriteLine("✅ NotificationWorkerService registered");
}
```

### 🎯 **EXERCISE 4.1**: Fix Registration

```
1. Mở Program.cs
2. Tìm AddHostedService<>
3. Kiểm tra đã thêm NotificationWorkerService chưa
4. Nếu không, thêm dòng:
   builder.Services.AddHostedService<NotificationWorkerService>();
5. Rebuild & Run
6. Kiểm tra log: có "NotificationWorkerService starting" không
```

---

## 4.2 ISSUE: Execution Overlap (Data Corruption)

### ❌ **PROBLEM**: Worker executes at the same time

**Triệu chứng:**
```
Database Error: Violation of PRIMARY KEY constraint
Message duplication
Out of order execution
```

**Nguyên nhân:**
```
❌ Nếu code như này:

private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    // ❌ Không có semaphore check
    await ProcessDueNotificationsAsync(ct);
}

Flow:
- 14:00:00 - Tick 1 start → Query DB (1 phút)
- 14:01:00 - Tick 2 start → Query DB again (Tick 1 chưa xong!)
- 💥 Duplicate: Cùng một notification được xử lý 2 lần
```

### ✅ **SOLUTION**: Thêm Semaphore Guard

**File**: `NotificationWorkerService.cs`

**Trước (sai):**
```csharp
private SemaphoreSlim _gate = new(1, 1); // Declared nhưng không dùng

private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    var sw = Stopwatch.StartNew();
    try
    {
        await ProcessDueNotificationsAsync(ct); // ❌ Không check gate
    }
    finally
    {
        sw.Stop();
        _logger.LogDebug("Finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
        // ❌ Không Release
    }
}
```

**Sau (đúng):**
```csharp
private readonly SemaphoreSlim _gate = new(1, 1);

private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    // ✅ Try to acquire semaphore (non-blocking)
    if (!await _gate.WaitAsync(0, ct))
    {
        _logger.LogWarning("Previous execution still running; skipping this tick.");
        return;
    }
    
    var sw = Stopwatch.StartNew();
    try
    {
        // ✅ Now safe to execute
        await ProcessDueNotificationsAsync(ct);
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in processing.");
    }
    finally
    {
        sw.Stop();
        _gate.Release(); // ✅ Always release
        _logger.LogDebug("Finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
    }
}
```

### 🎯 **EXERCISE 4.2**: Add Semaphore Check

```
1. Mở NotificationWorkerService.cs
2. Tìm method SafeProcessOnceAsync()
3. Thêm kiểm tra semaphore trước khi execute:
   if (!await _gate.WaitAsync(0, ct))
   {
       _logger.LogWarning("Previous execution still running");
       return;
   }
4. Thêm _gate.Release() trong finally{}
5. Test: Nếu execution mất lâu, log phải hiển thị skip message
```

---

## 4.3 ISSUE: Worker Crashes Silently

### ❌ **PROBLEM**: Exception trong worker không được log

**Triệu chứng:**
```
- Worker không run lần thứ 2
- Không có error log
- Silent failure
```

**Nguyên nhân:**
```csharp
// ❌ WRONG: Exception không được catch
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var timer = new PeriodicTimer(_interval);
    
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await ProcessDueNotificationsAsync(stoppingToken); // ❌ Exception not caught
        // 💥 If exception here → loop exits, worker stops
    }
    
    timer.Dispose();
}
```

### ✅ **SOLUTION**: Proper Exception Handling

**File**: `NotificationWorkerService.cs`

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("NotificationWorkerService starting.");
    
    var startupJitterMs = Random.Shared.Next(0, 5000);
    try
    {
        await Task.Delay(startupJitterMs, stoppingToken);
    }
    catch (OperationCanceledException)
    {
        return;
    }
    
    var timer = new PeriodicTimer(_interval);
    
    try
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // ✅ Call safe method with exception handling
            await SafeProcessOnceAsync(stoppingToken);
        }
    }
    catch (OperationCanceledException)
    {
        // ✅ Expected when app shutdown
        _logger.LogInformation("Worker cancelled gracefully.");
    }
    catch (Exception ex)
    {
        // ✅ Catch unexpected exceptions
        _logger.LogError(ex, "NotificationWorkerService crashed unexpectedly!");
    }
    finally
    {
        timer.Dispose();
        _logger.LogInformation("NotificationWorkerService stopping.");
    }
}

private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    if (!await _gate.WaitAsync(0, ct))
    {
        _logger.LogWarning("Previous execution still running; skipping.");
        return;
    }
    
    var sw = Stopwatch.StartNew();
    try
    {
        await ProcessDueNotificationsAsync(ct);
    }
    catch (OperationCanceledException)
    {
        // Expected
    }
    catch (Exception ex)
    {
        // ✅ Log và continue (không throw)
        _logger.LogError(ex, "Error in processing.");
    }
    finally
    {
        sw.Stop();
        _gate.Release();
        _logger.LogDebug("Finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
    }
}
```

### 🎯 **EXERCISE 4.3**: Add Exception Handling

```
1. Mở NotificationWorkerService.cs
2. Kiểm tra ExecuteAsync() có try-catch-finally không
3. Nếu không đầy đủ, thêm:
   try { }
   catch (OperationCanceledException) { }  // Expected
   catch (Exception ex) { _logger.LogError(...); }  // Unexpected
   finally { timer.Dispose(); }
4. Kiểm tra SafeProcessOnceAsync() có try-catch-finally không
5. Test: Tạo bug trong ProcessDueNotificationsAsync() để test exception handling
```

---

# ✨ BÀI 5: THÊM FEATURE MỚI

## 5.1 Feature: Thêm Email Notification Worker

### 📝 **REQUIREMENT**:
Tạo background worker gửi email notifications mỗi 10 phút (dựa trên NotificationWorkerService)

### 🎯 **STEPS**:

#### **Step 1: Create EmailNotificationWorkerService**

**File**: `WebUI/Workers/EmailNotificationWorkerService.cs`

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BLL.Interfaces;

namespace WebUI.Workers;

public class EmailNotificationWorkerService : BackgroundService
{
    private readonly ILogger<EmailNotificationWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    // Chạy mỗi 10 phút
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);
    
    // Semaphore: tránh overlap
    private readonly SemaphoreSlim _gate = new(1, 1);

    public EmailNotificationWorkerService(
        ILogger<EmailNotificationWorkerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailNotificationWorkerService starting.");

        // Random delay
        var startupJitterMs = Random.Shared.Next(0, 5000);
        try
        {
            await Task.Delay(startupJitterMs, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SafeProcessOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("EmailNotificationWorkerService cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailNotificationWorkerService crashed.");
        }
        finally
        {
            timer.Dispose();
            _logger.LogInformation("EmailNotificationWorkerService stopping.");
        }
    }

    private async Task SafeProcessOnceAsync(CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
        {
            _logger.LogWarning("Previous email send still running; skipping.");
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await ProcessPendingEmailsAsync(ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in email processing.");
        }
        finally
        {
            sw.Stop();
            _gate.Release();
            _logger.LogDebug("Email send finished in {ElapsedMs} ms.", sw.ElapsedMilliseconds);
        }
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken ct)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        
        // TODO: Get IEmailService from BLL
        // var emailService = scope.ServiceProvider
        //     .GetRequiredService<IEmailService>();
        
        // var count = await emailService.SendPendingAsync();
        
        // if (count > 0)
        // {
        //     _logger.LogInformation("Sent {Count} email(s).", count);
        // }
    }
}
```

#### **Step 2: Register in Program.cs**

```csharp
// Add this line in Program.cs
builder.Services.AddHostedService<EmailNotificationWorkerService>();
```

#### **Step 3: Test**

```
1. Run app
2. Check logs for: "EmailNotificationWorkerService starting"
3. Wait 10 minutes or modify interval to 1 minute for testing
4. Verify: "Sent X email(s)"
```

### ✅ **EXERCISE 5.1**: Thực hành tạo worker

```
1. Tạo file EmailNotificationWorkerService.cs
2. Copy code từ trên
3. Register trong Program.cs
4. Build & run
5. Check logs
```

---

## 5.2 Feature: Add Real-time Message Count to SignalR

### 📝 **REQUIREMENT**:
Gửi real-time unread message count cho mỗi user

### 🎯 **STEPS**:

#### **Step 1: Add Method to ChatHub**

**File**: `ChatHub.cs`

```csharp
// Add this method
public async Task NotifyUnreadCount(int conversationId, int unreadCount)
{
    var userId = Q(Context.GetHttpContext(), "userId");
    
    // Gửi unread count
    await Clients.Group($"user:{userId}")
        .SendAsync("unreadCountUpdated", conversationId, unreadCount);
}

// Modify SendMessage to update unread count
public async Task SendMessage(SendMessageDto dto)
{
    var message = await _chatService.SaveMessageAsync(dto);
    
    // ✅ Gửi message
    await Clients.Group($"user:{dto.RecipientId}")
        .SendAsync("receiveMessage", message);
    
    // ✅ Cập nhật unread count cho receiver
    var unreadCount = await _chatService.GetUnreadCountAsync(dto.RecipientId);
    await Clients.Group($"user:{dto.RecipientId}")
        .SendAsync("unreadCountUpdated", dto.ConversationId, unreadCount);
    
    // ✅ Confirmation cho sender
    await Clients.Caller
        .SendAsync("messageSent", message.Id);
}
```

#### **Step 2: Add JavaScript Handler**

```javascript
// Listen for unread count updates
connection.on("unreadCountUpdated", (conversationId, count) => {
    console.log(`Conversation ${conversationId}: ${count} unread`);
    
    // Update UI
    document.getElementById(`unread-${conversationId}`).textContent = count;
    
    // Update badge
    const badge = document.getElementById("total-unread");
    const currentCount = parseInt(badge.textContent) || 0;
    badge.textContent = currentCount + (count > 0 ? count : 0);
});
```

#### **Step 3: Test**

```
1. Open 2 browser windows
2. Send message from A to B
3. B should see unread count update in real-time
4. No page refresh needed
```

### ✅ **EXERCISE 5.2**: Thực hành thêm feature

```
1. Thêm method NotifyUnreadCount() vào ChatHub.cs
2. Modify SendMessage() để gửi unread count
3. Thêm JavaScript handler trong chat.js
4. Test: Open 2 windows, send message, check real-time update
```

---

# 🧪 BÀI 6: INTEGRATION & TESTING

## 6.1 Test SignalR Integration

### **Test Case 1: Basic Message Flow**

```csharp
[Fact]
public async Task SendMessage_ShouldBroadcastToRecipient()
{
    // Arrange
    var mockChatService = new Mock<IChatService>();
    mockChatService
        .Setup(x => x.SaveMessageAsync(It.IsAny<SendMessageDto>()))
        .ReturnsAsync(new MessageDto { Id = 1, Content = "Test" });
    
    var mockLogger = new Mock<ILogger<ChatHub>>();
    var hub = new ChatHub(mockChatService.Object, mockLogger.Object);
    
    // Mock Hub context
    var mockContext = new Mock<HubCallerContext>();
    var mockClients = new Mock<IHubCallerClients>();
    var mockClientProxy = new Mock<IClientProxy>();
    
    hub.Context = mockContext.Object;
    hub.Clients = mockClients.Object;
    
    mockClients
        .Setup(x => x.Group($"user:456"))
        .Returns(mockClientProxy.Object);
    
    // Act
    var dto = new SendMessageDto 
    { 
        SenderId = "123",
        RecipientId = "456",
        Content = "Test message"
    };
    await hub.SendMessage(dto);
    
    // Assert
    mockClientProxy.Verify(
        x => x.SendAsync("receiveMessage", It.IsAny<MessageDto>(), null, It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### ✅ **EXERCISE 6.1**: Unit Test SignalR

```
1. Tạo file: Tests/ChatHubTests.cs
2. Copy test case từ trên
3. Run test: dotnet test
4. Verify: Test should pass
```

---

## 6.2 Test Worker Integration

### **Test Case 2: Worker Execution**

```csharp
[Fact]
public async Task Worker_ShouldDispatchNotifications()
{
    // Arrange
    var mockService = new Mock<INotificationService>();
    mockService
        .Setup(x => x.DispatchDueAsync())
        .ReturnsAsync(5);
    
    var mockLogger = new Mock<ILogger<NotificationWorkerService>>();
    var mockServiceProvider = new Mock<IServiceProvider>();
    
    var scope = new Mock<IAsyncServiceScope>();
    scope.Setup(x => x.ServiceProvider.GetService(typeof(INotificationService)))
        .Returns(mockService.Object);
    
    mockServiceProvider
        .Setup(x => x.CreateAsyncScope())
        .Returns(scope.Object);
    
    var worker = new NotificationWorkerService(mockLogger.Object, mockServiceProvider.Object);
    
    // Act
    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(2)); // Run for 2 seconds
    
    await worker.ExecuteAsync(cts.Token);
    
    // Assert: Verify service was called at least once
    mockService.Verify(x => x.DispatchDueAsync(), Times.AtLeastOnce());
}
```

### ✅ **EXERCISE 6.2**: Unit Test Worker

```
1. Tạo file: Tests/NotificationWorkerTests.cs
2. Copy test case từ trên
3. Run test: dotnet test
4. Verify: Test should pass
```

---

## 6.3 Integration Test: SignalR + Worker

### **Test Case 3: Worker Broadcasts to SignalR**

```csharp
[Fact]
public async Task Worker_ShouldBroadcastToSignalR_WhenProcessing()
{
    // Arrange
    var mockNotificationService = new Mock<INotificationService>();
    mockNotificationService
        .Setup(x => x.DispatchDueAsync())
        .ReturnsAsync(3);
    
    var mockSignalRService = new Mock<ISignalRService>();
    mockSignalRService
        .Setup(x => x.BroadcastNotificationAsync(It.IsAny<NotificationDto>()))
        .ReturnsAsync(true);
    
    // Act: Simulate worker execution
    var notificationService = mockNotificationService.Object;
    var count = await notificationService.DispatchDueAsync();
    
    // Assert
    Assert.Equal(3, count);
    mockSignalRService.Verify(
        x => x.BroadcastNotificationAsync(It.IsAny<NotificationDto>()),
        Times.AtLeast(1));
}
```

### ✅ **EXERCISE 6.3**: Integration Test

```
1. Tạo test case ở trên
2. Run: dotnet test
3. Verify: Integration works correctly
```

---

## 6.4 Manual Testing Checklist

### **SignalR Testing**

```
□ Connection
  ✓ Open browser DevTools → Network
  ✓ Look for /chatHub WebSocket connection
  ✓ Verify connection state: (1 = Connected)

□ Message Flow
  ✓ Open 2 windows: A (customer) & B (staff)
  ✓ A sends message
  ✓ B receives in real-time (no refresh)
  ✓ B replies
  ✓ A receives in real-time

□ Presence
  ✓ When staff goes online → customers see "Staff Online"
  ✓ When staff goes offline → customers see "Staff Offline"

□ Groups
  ✓ A sends to B via group → only B receives
  ✓ A sends via Clients.All → everyone receives

□ Error Handling
  ✓ Close browser → OnDisconnectedAsync called
  ✓ Reconnect → OnConnectedAsync called
  ✓ Network error → Auto-reconnect with backoff
```

### **Worker Testing**

```
□ Notification Worker
  ✓ App starts → Log: "NotificationWorkerService starting"
  ✓ Every 1 minute → Log: "Processed X notifications"
  ✓ If execution > 1 min → Log: "Previous execution still running; skipping"
  ✓ App shuts down → Log: "NotificationWorkerService stopping"

□ Promotion Worker
  ✓ App starts → Log: "PromotionWorkerService starting"
  ✓ Every 5 minutes → Check expired promotions
  ✓ Expired promotion → Auto set to "Inactive"
  ✓ App shuts down → Log: "PromotionWorkerService stopping"

□ Database
  ✓ Notifications table → New UserNotifications created
  ✓ Promotions table → Expired promotions are inactive
  ✓ No duplicate records → Semaphore is working
```

---

# 📋 QUICK REFERENCE FIXES

## Common Issues & Solutions

### **Issue 1: Message Not Sent**
```csharp
❌ WRONG:
await Clients.Group($"user:{dto.SenderId}").SendAsync(...)

✅ CORRECT:
await Clients.Group($"user:{dto.RecipientId}").SendAsync(...)
```

### **Issue 2: SignalR Connection Fails**
```csharp
✅ FIX in Program.cs:
.AllowCredentials() // Add this line in CORS
```

### **Issue 3: Worker Execution Overlaps**
```csharp
✅ FIX:
if (!await _gate.WaitAsync(0, ct)) return; // Check semaphore
```

### **Issue 4: Worker Not Starting**
```csharp
✅ FIX in Program.cs:
builder.Services.AddHostedService<NotificationWorkerService>();
```

### **Issue 5: Scoped Service in Worker**
```csharp
✅ FIX:
await using var scope = _serviceProvider.CreateAsyncScope();
var service = scope.ServiceProvider.GetRequiredService<IService>();
```

---

# 🎓 LEARNING RECAP

## SignalR Essentials ✅
- ✅ Hub = Server-side SignalR controller
- ✅ OnConnectedAsync = Client connects
- ✅ OnDisconnectedAsync = Client disconnects
- ✅ Groups = Targeted messaging
- ✅ Clients.Group() = Send to specific group
- ✅ Clients.All = Broadcast to all
- ✅ Context.Abort() = Reject connection

## Workers Essentials ✅
- ✅ BackgroundService = Base class for workers
- ✅ ExecuteAsync() = Main loop
- ✅ PeriodicTimer = Scheduled execution
- ✅ SemaphoreSlim = Prevent overlap
- ✅ CancellationToken = Graceful shutdown
- ✅ CreateAsyncScope() = Get scoped services
- ✅ try-catch-finally = Error handling

## Integration ✅
- ✅ Worker can call SignalR to broadcast
- ✅ SignalR can listen to worker events
- ✅ Both need proper error handling
- ✅ Both need logging for debugging

---

# 🎯 EXERCISES SUMMARY

| Bài | Exercise | Độ Khó | Thời Gian |
|-----|----------|--------|----------|
| 1.1 | Quiz: ConcurrentDictionary | ⭐ | 5 phút |
| 1.2 | Draw flow diagram | ⭐ | 10 phút |
| 1.3 | Explain Groups | ⭐⭐ | 10 phút |
| 2.1 | Fix SendMessage receiver | ⭐ | 5 phút |
| 2.2 | Complete OnDisconnected | ⭐⭐ | 15 phút |
| 2.3 | Add CORS AllowCredentials | ⭐ | 5 phút |
| 3.1 | Understand Semaphore | ⭐⭐ | 15 phút |
| 3.2 | Understand ExecuteAsync flow | ⭐⭐ | 15 phút |
| 3.3 | Quiz: Worker concepts | ⭐⭐ | 10 phút |
| 4.1 | Fix worker registration | ⭐ | 5 phút |
| 4.2 | Add semaphore check | ⭐⭐ | 15 phút |
| 4.3 | Add exception handling | ⭐⭐ | 15 phút |
| 5.1 | Create Email Worker | ⭐⭐⭐ | 30 phút |
| 5.2 | Add unread count feature | ⭐⭐⭐ | 30 phút |
| 6.1 | Unit test SignalR | ⭐⭐⭐ | 30 phút |
| 6.2 | Unit test Worker | ⭐⭐⭐ | 30 phút |
| 6.3 | Integration test | ⭐⭐⭐ | 30 phút |

**Total Time**: ~4-5 hours (từ beginner → intermediate)

---

# 📖 NEXT STEPS

1. **Bắt đầu**: Làm Exercise 1.1 - 1.3 (hiểu khái niệm)
2. **Sửa code**: Làm Exercise 2.1 - 2.3 (fix ChatHub)
3. **Hiểu worker**: Làm Exercise 3.1 - 3.3 (hiểu code)
4. **Sửa worker**: Làm Exercise 4.1 - 4.3 (fix issues)
5. **Thêm feature**: Làm Exercise 5.1 - 5.2 (tạo mới)
6. **Testing**: Làm Exercise 6.1 - 6.3 (test code)

---

**Status**: ✅ Ready to Learn & Code!  
**Difficulty**: Beginner → Intermediate  
**Time Commitment**: 4-5 hours  
**Success Criteria**: Able to fix & modify ChatHub & Workers independently
