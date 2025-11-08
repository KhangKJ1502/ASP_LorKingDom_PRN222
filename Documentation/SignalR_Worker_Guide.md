# 📡 SignalR & Background Worker - Hướng Dẫn Toàn Diện

## 📑 Mục lục
1. [SignalR - Real-time Chat](#signalr---real-time-chat)
2. [Background Workers](#background-workers)
3. [Architecture](#architecture)
4. [Implementation Details](#implementation-details)
5. [Best Practices](#best-practices)

---

## SignalR - Real-time Chat

### 1. **Khái Niệm Cơ Bản**

**SignalR** là một thư viện ASP.NET Core cho phép **two-way communication** (trao đổi hai chiều) giữa server và client qua:
- **WebSocket** (ưu tiên - real-time, lâu dài)
- **Server-Sent Events (SSE)**
- **Long polling** (fallback)

### 2. **Cách SignalR Hoạt Động Trong Dự Án**

```
┌─────────────────┐                    ┌─────────────────┐
│   Browser       │                    │   Browser       │
│   (Customer)    │                    │   (Staff)       │
└────────┬────────┘                    └────────┬────────┘
         │                                      │
         │  WebSocket                           │
         │  /chatHub?userId=123                 │  WebSocket
         │                                      │  /chatHub?userId=456&isStaff=true
         │                                      │
         └──────────┬──────────┬────────────────┘
                    │          │
                    ▼          ▼
            ┌─────────────────────────┐
            │    ChatHub (SignalR)    │
            │  - OnConnectedAsync()   │
            │  - SendMessage()        │
            │  - BroadcastPresence()  │
            └─────────────────────────┘
                    │
                    ▼
            ┌─────────────────────────┐
            │   IChatService (BLL)    │
            │  - SaveMessageAsync()   │
            │  - GetConversations()   │
            └─────────────────────────┘
```

### 3. **ChatHub - Chính Hub**

#### **File**: `WebUI/ChatHubs/ChatHub.cs`

```csharp
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    // In-memory tracking
    private static readonly ConcurrentDictionary<string, int> _staffConnCount;
    private static readonly ConcurrentDictionary<string, string> _staffNames;
    private static readonly ConcurrentDictionary<string, bool> _staffPageState;
}
```

#### **Lifecycle Events**

| Event | Mục đích | Code |
|-------|---------|------|
| **OnConnectedAsync()** | Khi client kết nối | Validate, đăng ký group |
| **OnDisconnectedAsync()** | Khi client disconnect | Cleanup, broadcast offline |
| **Custom Methods** | Nhận message từ client | SendMessage(), MarkAsRead() |

#### **OnConnectedAsync() - Chi tiết**

```csharp
public override async Task OnConnectedAsync()
{
    try
    {
        // 1. Lấy query parameters từ URL
        var http = Context.GetHttpContext();
        var userId = Q(http, "userId");        // Bắt buộc
        var isStaff = QBool(http, "isStaff");  // Có phải staff?
        var name = Q(http, "name");             // Tên hiển thị

        // 2. Validate
        if (string.IsNullOrWhiteSpace(userId))
        {
            Context.Abort(); // Từ chối kết nối
            return;
        }

        // 3. Lưu vào database
        await _chatService.UserConnectedAsync(
            userId, isStaff, name, Context.ConnectionId);

        // 4. Thêm vào group
        await Groups.AddToGroupAsync(
            Context.ConnectionId, $"user:{userId}");

        // 5. Nếu là staff
        if (isStaff)
        {
            // Track staff status
            _staffNames[userId] = name;
            _staffConnCount.AddOrUpdate(userId, 1, (_, n) => n + 1);
            _staffPageState[userId] = true;

            // Broadcast staff online đến tất cả
            if (_staffConnCount[userId] == 1) // Lần đầu online
            {
                await Clients.All.SendAsync("staffPresence", new
                {
                    staffId = userId,
                    name = _staffNames[userId],
                    online = true
                });
            }

            // Gửi danh sách conversations cho staff
            var conversations = await _chatService
                .GetStaffConversationsAsync(userId);
            await Clients.Caller.SendAsync(
                "conversationList", conversations);
        }

        // 6. Broadcast presence
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

### 4. **Groups - Gửi Tin Nhắn Targeted**

**SignalR Groups** cho phép gửi tin nhắn đến một tập hợp users cụ thể:

```csharp
// Thêm vào group
await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

// Gửi tin nhắn đến một user
await Clients.Group($"user:{userId}")
    .SendAsync("receiveMessage", message);

// Gửi đến tất cả (broadcast)
await Clients.All.SendAsync("presenceChanged", userId, isOnline);

// Gửi đến tất cả EXCEPT caller
await Clients.Others.SendAsync("userTyping", userId);
```

### 5. **Client-side Integration**

#### **JavaScript - Kết nối Hub**

```javascript
const userId = "123";
const isStaff = false;

// Tạo connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub?userId=" + userId + "&isStaff=" + isStaff)
    .withAutomaticReconnect()
    .build();

// Khi connected
connection.onreconnected(() => {
    console.log("✅ Reconnected to Chat Hub");
});

// Khi disconnected
connection.onclose(() => {
    console.log("❌ Disconnected from Chat Hub");
});

// Start connection
connection.start()
    .then(() => console.log("🚀 Connected to Chat Hub"))
    .catch(err => console.error("❌ Connection error:", err));
```

#### **Nhận Tin Nhắn**

```javascript
// Nhận tin nhắn
connection.on("receiveMessage", (message) => {
    console.log("💬 New message:", message);
    // Append to UI
    document.getElementById("messages").innerHTML += 
        `<p>${message.senderName}: ${message.content}</p>`;
});

// Nhận presence change
connection.on("presenceChanged", (userId, isOnline) => {
    console.log(`User ${userId} is ${isOnline ? 'online' : 'offline'}`);
});

// Nhận staff presence
connection.on("staffPresence", (data) => {
    console.log(`Staff ${data.name} (${data.staffId}) is ${data.online ? 'ONLINE' : 'OFFLINE'}`);
});
```

#### **Gửi Tin Nhắn**

```javascript
function sendMessage(content) {
    connection.invoke("SendMessage", {
        conversationId: 1,
        content: content,
        recipientId: "456"
    })
    .then(() => console.log("✅ Message sent"))
    .catch(err => console.error("❌ Error sending message:", err));
}
```

### 6. **Program.cs Configuration**

```csharp
// 1. Thêm SignalR service
builder.Services.AddSignalR();

// 2. Map SignalR Hub tại route /chatHub
app.MapHub<ChatHub>("/chatHub");

// 3. CORS config (nếu dùng external client)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins("https://example.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // IMPORTANT: SignalR dùng credentials
    });
});
```

### 7. **Các Methods Trong ChatHub**

```csharp
// Gửi tin nhắn 1-1
public async Task SendMessage(SendMessageDto dto)
{
    var message = await _chatService.SaveMessageAsync(dto);
    
    // Gửi cho receiver
    await Clients.Group($"user:{dto.RecipientId}")
        .SendAsync("receiveMessage", message);
    
    // Gửi confirmation cho sender
    await Clients.Caller.SendAsync("messageSent", message.Id);
}

// Đánh dấu đã đọc
public async Task MarkAsRead(int conversationId)
{
    var userId = Q(Context.GetHttpContext(), "userId");
    await _chatService.MarkAsReadAsync(conversationId, userId);
    
    // Notify sender
    await Clients.All.SendAsync(
        "messagesRead", conversationId, userId);
}

// Broadcast typing indicator
public async Task UserTyping(int conversationId, string userId)
{
    await Clients.Others.SendAsync(
        "userTyping", conversationId, userId);
}

// Disconnect handler
public override async Task OnDisconnectedAsync(Exception? exception)
{
    var userId = Q(Context.GetHttpContext(), "userId");
    var isStaff = QBool(Context.GetHttpContext(), "isStaff");
    
    if (isStaff && _staffConnCount.TryGetValue(userId, out var count))
    {
        if (count == 1)
        {
            // Broadcast staff offline
            await Clients.All.SendAsync("staffPresence", new
            {
                staffId = userId,
                online = false
            });
        }
    }
    
    await BroadcastPresence(userId, false);
    await base.OnDisconnectedAsync(exception);
}
```

---

## Background Workers

### 1. **Khái Niệm Cơ Bản**

**Background Workers** (IHostedService) là dịch vụ chạy ngầm trong ứng dụng ASP.NET Core:
- Chạy liên tục trong background
- Không chặn request xử lý
- Có lifecycle riêng (Start/Stop)
- Perfect cho scheduled tasks

### 2. **Worker Architecture Trong Dự Án**

```
┌─────────────────────────────────────┐
│   Program.cs Startup                │
├─────────────────────────────────────┤
│ builder.Services.AddHostedService   │
│   <NotificationWorkerService>       │
│ builder.Services.AddHostedService   │
│   <PromotionWorkerService>          │
└────────────┬────────────────────────┘
             │
             ├─────────────────────────┐
             │                         │
             ▼                         ▼
    ┌──────────────────┐      ┌──────────────────┐
    │NotificationWorker│      │PromotionWorker   │
    │- Interval: 1 min │      │- Interval: 5 min │
    │- DispatchDue()   │      │- CheckExpired()  │
    └──────────────────┘      └──────────────────┘
             │                         │
             ▼                         ▼
    ┌──────────────────┐      ┌──────────────────┐
    │INotificationSvc  │      │IPromotionService │
    │Business Logic    │      │Business Logic    │
    └──────────────────┘      └──────────────────┘
             │                         │
             ▼                         ▼
    ┌──────────────────┐      ┌──────────────────┐
    │Database          │      │Database          │
    │- Notification   │      │- Promotion       │
    │- UserNotification│      │- Product         │
    └──────────────────┘      └──────────────────┘
```

### 3. **NotificationWorkerService - Chi tiết**

#### **File**: `WebUI/Workers/NotificationWorkerService.cs`

```csharp
public class NotificationWorkerService : BackgroundService
{
    private readonly ILogger<NotificationWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Chạy mỗi 1 phút
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    
    // Semaphore: chỉ cho phép 1 execution tại cùng 1 lúc
    private readonly SemaphoreSlim _gate = new(1, 1);

    public NotificationWorkerService(
        ILogger<NotificationWorkerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // Main loop - chạy khi app start
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorkerService starting.");

        // 1. Delay ngẫu nhiên trước khi bắt đầu (tránh spike)
        var startupJitterMs = Random.Shared.Next(0, 5000);
        try
        {
            await Task.Delay(startupJitterMs, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return; // App shutdown
        }

        // 2. Tạo timer
        var timer = new PeriodicTimer(_interval);

        try
        {
            // 3. Loop: chạy mỗi 1 phút
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SafeProcessOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationWorkerService crashed unexpectedly.");
        }
        finally
        {
            timer.Dispose();
            _logger.LogInformation("NotificationWorkerService stopping.");
        }
    }

    // Xử lý an toàn: không cho overlap
    private async Task SafeProcessOnceAsync(CancellationToken ct)
    {
        // Nếu execution trước đó vẫn chưa xong, bỏ qua tick này
        if (!await _gate.WaitAsync(0, ct))
        {
            _logger.LogWarning(
                "Previous notification dispatch still running; skipping this tick.");
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await ProcessDueNotificationsAsync(ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while processing due notifications.");
        }
        finally
        {
            sw.Stop();
            _gate.Release(); // Release semaphore
            _logger.LogDebug(
                "Dispatch run finished in {ElapsedMs} ms.",
                sw.ElapsedMilliseconds);
        }
    }

    // Business logic: gửi thông báo đã đến hạn
    private async Task ProcessDueNotificationsAsync(CancellationToken ct)
    {
        // Tạo scope để sử dụng DI services (Scoped)
        await using var scope = _serviceProvider.CreateAsyncScope();
        var notificationService = scope.ServiceProvider
            .GetRequiredService<INotificationService>();

        // Gọi service method
        var count = await notificationService.DispatchDueAsync();

        if (count > 0)
        {
            _logger.LogInformation(
                "Processed {Count} due notification(s) at {UtcTime}.",
                count,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    // Called when app shutdown
    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NotificationWorkerService is stopping gracefully.");
        return base.StopAsync(stoppingToken);
    }
}
```

### 4. **PromotionWorkerService - Chi tiết**

#### **File**: `WebUI/Workers/PromotionWorkerService.cs`

```csharp
public class PromotionWorkerService : BackgroundService
{
    private readonly ILogger<PromotionWorkerService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Kiểm tra mỗi 5 phút
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _gate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PromotionWorkerService starting. Check interval: {Interval}",
            _interval);

        // Delay ngẫu nhiên
        var startupJitterMs = Random.Shared.Next(0, 3000);
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
            // Chạy ngay lần đầu (không chờ interval)
            await SafeProcessOnceAsync(stoppingToken);

            // Sau đó chạy theo interval
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SafeProcessOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PromotionWorkerService cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PromotionWorkerService crashed unexpectedly.");
        }
        finally
        {
            timer.Dispose();
            _logger.LogInformation("PromotionWorkerService stopping.");
        }
    }

    private async Task SafeProcessOnceAsync(CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
        {
            _logger.LogWarning(
                "Previous promotion check still running; skipping this tick.");
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await ProcessExpiredPromotionsAsync(ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while processing expired promotions.");
        }
        finally
        {
            sw.Stop();
            _gate.Release();
            _logger.LogDebug(
                "Promotion check finished in {ElapsedMs} ms.",
                sw.ElapsedMilliseconds);
        }
    }

    private async Task ProcessExpiredPromotionsAsync(CancellationToken ct)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var promotionService = scope.ServiceProvider
            .GetRequiredService<IPromotionService>();

        try
        {
            var activePromotions = await promotionService.GetActiveAsync();
            var now = DateTime.Now;
            int expiredCount = 0;

            foreach (var promo in activePromotions)
            {
                // Nếu đã hết hạn
                if (now > promo.EndDate)
                {
                    _logger.LogInformation(
                        "Promotion '{Code}' (ID: {Id}) has expired.",
                        promo.PromotionCode, promo.PromotionId);

                    try
                    {
                        // Tự động set về Inactive
                        var updateDto = new PromotionUpdateDto
                        {
                            PromotionId = promo.PromotionId,
                            PromotionCode = promo.PromotionCode,
                            Description = promo.Description,
                            DiscountPercent = promo.DiscountPercent,
                            StartDate = promo.StartDate,
                            EndDate = promo.EndDate,
                            Status = "Inactive"
                        };

                        await promotionService.UpdateAsync(updateDto);
                        expiredCount++;

                        _logger.LogInformation(
                            "Successfully set promotion '{Code}' to Inactive.",
                            promo.PromotionCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to set promotion '{Code}' to Inactive.",
                            promo.PromotionCode);
                    }
                }
            }

            if (expiredCount > 0)
            {
                _logger.LogInformation(
                    "Processed {Count} expired promotion(s).",
                    expiredCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in ProcessExpiredPromotionsAsync");
        }
    }
}
```

### 5. **Đăng Ký Workers Trong Program.cs**

```csharp
// Background Workers
builder.Services.AddHostedService<NotificationWorkerService>();
builder.Services.AddHostedService<WebUI.Workers.PromotionWorkerService>();
```

### 6. **Key Concepts - Semaphore & PeriodicTimer**

#### **Semaphore (Tránh Overlap)**

```csharp
private readonly SemaphoreSlim _gate = new(1, 1); // Max 1 concurrent

// Cách sử dụng
if (!await _gate.WaitAsync(0, ct)) // Non-blocking wait
{
    _logger.LogWarning("Previous execution still running");
    return; // Skip this tick
}

try
{
    // Do work
}
finally
{
    _gate.Release(); // Release semaphore
}
```

**Tại sao cần Semaphore?**
- Nếu execution trước mất >1 phút (chậm), tick tiếp theo sẽ bị skip
- Tránh data race condition khi 2 execution chạy cùng lúc
- Đảm bảo database integrity

#### **PeriodicTimer (Thay vì Timer)**

```csharp
// OLD (không tốt)
var timer = new System.Timers.Timer(interval);
timer.Elapsed += (s, e) => { /* execute */ };
timer.Start();

// NEW (tốt hơn - .NET 6+)
var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
while (await timer.WaitForNextTickAsync(cancellationToken))
{
    await SafeProcessOnceAsync(cancellationToken);
}
timer.Dispose();
```

**Lợi ích:**
- Tích hợp `CancellationToken` (graceful shutdown)
- Precise timing
- Tránh race conditions
- Async-friendly

---

## Architecture

### 1. **Layer Diagram**

```
┌─────────────────────────────────────────────────────┐
│         WebUI (Presentation Layer)                  │
├─────────────────────────────────────────────────────┤
│  Controllers  │ Views  │ ChatHub (SignalR)  │ Workers│
└────────────────────┬────────────────────────────────┘
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────┐
│         BLL (Business Logic Layer)                  │
├─────────────────────────────────────────────────────┤
│ INotificationService  │ IChatService  │ IPromoService│
│ NotificationService   │ ChatService   │ PromotionSvc │
└────────────────────┬────────────────────────────────┘
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────┐
│         DAL (Data Access Layer)                     │
├─────────────────────────────────────────────────────┤
│ INotificationRepo  │ IChatRepo  │ IPromotionRepo    │
│ Repositories + DbContext                            │
└────────────────────┬────────────────────────────────┘
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────┐
│         SQL Server Database                         │
└─────────────────────────────────────────────────────┘
```

### 2. **Data Flow - SignalR Message**

```
Client (Browser)
    │
    ├─ WebSocket /chatHub?userId=123
    │
    ▼
ChatHub.SendMessage(dto)
    │
    ├─ Save to DB via IChatService
    │    ├─ _chatService.SaveMessageAsync(dto)
    │    └─ Calls IMessageRepository.AddAsync()
    │
    ├─ Broadcast to Recipient
    │    └─ await Clients.Group("user:456")
    │         .SendAsync("receiveMessage", message)
    │
    └─ Send Confirmation to Sender
         └─ await Clients.Caller
              .SendAsync("messageSent", message.Id)

Recipient (Browser)
    │
    ├─ Listen connection.on("receiveMessage")
    │
    └─ Update UI with new message
```

### 3. **Data Flow - Background Worker**

```
App Startup
    │
    ├─ Register Workers in DI
    │    ├─ AddHostedService<NotificationWorkerService>()
    │    └─ AddHostedService<PromotionWorkerService>()
    │
    ▼
App Running
    │
    ├─ NotificationWorkerService.ExecuteAsync()
    │    │
    │    ├─ Wait 1 minute
    │    │
    │    ├─ ProcessDueNotificationsAsync()
    │    │    │
    │    │    ├─ Acquire Semaphore (check if not running)
    │    │    │
    │    │    ├─ Create Scope & Get INotificationService
    │    │    │
    │    │    ├─ Call notificationService.DispatchDueAsync()
    │    │    │    │
    │    │    │    ├─ Query DB: Notifications where IsSent=false & ScheduledAt <= now
    │    │    │    │
    │    │    │    ├─ For each notification:
    │    │    │    │    ├─ BuildRecipientsAsync() → get user list
    │    │    │    │    ├─ Create UserNotification records
    │    │    │    │    └─ Set IsSent = true
    │    │    │    │
    │    │    │    └─ Return count
    │    │    │
    │    │    ├─ Log results
    │    │    │
    │    │    └─ Release Semaphore
    │    │
    │    └─ Loop (repeat every 1 minute)
    │
    └─ PromotionWorkerService.ExecuteAsync()
         │
         ├─ Wait 5 minutes
         │
         ├─ ProcessExpiredPromotionsAsync()
         │    │
         │    ├─ Get all Active promotions
         │    │
         │    ├─ For each promotion:
         │    │    ├─ If EndDate < now
         │    │    │    └─ Set Status = "Inactive"
         │    │    │
         │    │    └─ Log
         │    │
         │    └─ Return count
         │
         └─ Loop (repeat every 5 minutes)
```

---

## Implementation Details

### 1. **Scoped Services in Workers**

**Problem**: Services registered as `Scoped` không thể inject trực tiếp vào `Singleton` (workers).

**Solution**: Tạo `AsyncScope` trong mỗi execution:

```csharp
// ❌ WRONG
public class NotificationWorkerService : BackgroundService
{
    private readonly INotificationService _service; // ❌ Cannot inject Scoped into Singleton

    public NotificationWorkerService(INotificationService service)
    {
        _service = service; // ❌ Runtime error
    }
}

// ✅ CORRECT
public class NotificationWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider; // ✅ Inject IServiceProvider

    public NotificationWorkerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private async Task ProcessDueNotificationsAsync(CancellationToken ct)
    {
        // Tạo scope trong execution
        await using var scope = _serviceProvider.CreateAsyncScope();
        var notificationService = scope.ServiceProvider
            .GetRequiredService<INotificationService>(); // ✅ Get inside scope
        
        await notificationService.DispatchDueAsync();
    }
}
```

### 2. **CancellationToken - Graceful Shutdown**

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    try
    {
        // ✅ Pass stoppingToken để app có thể shutdown gracefully
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SafeProcessOnceAsync(stoppingToken);
        }
    }
    catch (OperationCanceledException)
    {
        // ✅ Handle graceful cancellation
        _logger.LogInformation("Worker cancelled gracefully");
    }
}
```

### 3. **Exception Handling Pattern**

```csharp
private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    if (!await _gate.WaitAsync(0, ct))
    {
        _logger.LogWarning("Previous execution still running");
        return;
    }

    try
    {
        await ProcessDueNotificationsAsync(ct);
    }
    catch (OperationCanceledException)
    {
        // ✅ Expected when app shuts down
        _logger.LogInformation("Task cancelled");
    }
    catch (Exception ex)
    {
        // ✅ Log errors but don't crash the worker
        _logger.LogError(ex, "Error in task execution");
    }
    finally
    {
        _gate.Release(); // ✅ Always release semaphore
    }
}
```

### 4. **Logging Pattern**

```csharp
// Enable detailed logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Console output
// ℹ️ NotificationWorkerService starting.
// ℹ️ Processed 5 due notification(s) at 2025-11-05 14:32:00
// 🔧 Dispatch run finished in 245 ms.
// ℹ️ NotificationWorkerService stopping.
```

---

## Best Practices

### 1. **SignalR Best Practices**

| ✅ Do | ❌ Don't |
|------|---------|
| Use Groups cho targeted messages | Broadcast đến tất cả nếu không cần |
| Validate input từ client | Trust client data |
| Add authentication/authorization | Allow anonymous connections |
| Handle reconnect scenarios | Assume stable connection |
| Use OnDisconnected để cleanup | Leave resources hanging |
| Compress large messages | Send raw data |
| Log important events | Silent errors |

### 2. **Worker Best Practices**

| ✅ Do | ❌ Don't |
|------|---------|
| Use SemaphoreSlim để tránh overlap | Allow concurrent executions |
| Create AsyncScope cho Scoped services | Inject Scoped into Singleton |
| Pass CancellationToken | Ignore shutdown signals |
| Log execution metrics | Silent failures |
| Use PeriodicTimer | Use old Timer class |
| Catch & log exceptions | Crash the worker |
| Release resources in finally | Memory leaks |

### 3. **Performance Tips**

#### **SignalR**
```csharp
// 1. Use binary protocol for large data
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// 2. Batch messages
await Clients.All.SendAsync("batchMessages", messages);

// 3. Compression
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 64; // 64KB
    options.StreamBufferCapacity = 32;
});
```

#### **Workers**
```csharp
// 1. Adjust intervals based on load
private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Not too frequent

// 2. Batch database operations
var count = await notificationService.DispatchBatch(batchSize: 200);

// 3. Add telemetry
var sw = Stopwatch.StartNew();
// ... do work ...
_telemetry.TrackMetric("NotificationDispatchTime", sw.ElapsedMilliseconds);
```

### 4. **Error Handling Strategy**

```csharp
// Pattern: Log, Retry, Continue
private async Task ProcessDueNotificationsAsync(CancellationToken ct)
{
    const int maxRetries = 3;
    int attempt = 0;
    
    while (attempt < maxRetries)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var service = scope.ServiceProvider
                .GetRequiredService<INotificationService>();
            
            var count = await service.DispatchDueAsync();
            
            if (count > 0)
            {
                _logger.LogInformation("Processed {Count} notifications", count);
            }
            
            return; // Success
        }
        catch (DbUpdateException ex) when (attempt < maxRetries - 1)
        {
            attempt++;
            _logger.LogWarning(
                "Database error (attempt {Attempt}/{Max}): {Message}",
                attempt, maxRetries, ex.Message);
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in notification processing");
            throw;
        }
    }
}
```

### 5. **Testing Workers**

```csharp
[Fact]
public async Task Worker_ProcessesNotifications_WhenDue()
{
    // Arrange
    var mockService = new Mock<INotificationService>();
    mockService
        .Setup(x => x.DispatchDueAsync())
        .ReturnsAsync(5);
    
    var serviceProvider = new Mock<IServiceProvider>();
    serviceProvider
        .Setup(x => x.GetService(typeof(INotificationService)))
        .Returns(mockService.Object);
    
    var worker = new NotificationWorkerService(
        new Mock<ILogger<NotificationWorkerService>>().Object,
        serviceProvider.Object);
    
    // Act
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await worker.ExecuteAsync(cts.Token);
    
    // Assert
    mockService.Verify(
        x => x.DispatchDueAsync(),
        Times.AtLeastOnce());
}
```

---

## Troubleshooting

### **SignalR Issues**

| Problem | Cause | Solution |
|---------|-------|----------|
| Connection fails | CORS not configured | Add `.AllowCredentials()` |
| Messages not received | Not in correct group | Check group name format |
| Reconnect loop | Network issues | Implement backoff in client |
| High latency | Message size | Use compression, batch |

### **Worker Issues**

| Problem | Cause | Solution |
|---------|-------|----------|
| Worker not starting | Service not registered | Check `AddHostedService()` |
| Execution overlaps | No semaphore | Add `SemaphoreSlim` lock |
| Database errors | Connection pool exhausted | Increase pool size |
| Memory leak | Resources not released | Use `finally` to cleanup |

---

## Summary

### **SignalR**
- ✅ Real-time bidirectional communication
- ✅ Uses WebSocket, SSE, or Long Polling
- ✅ Perfect for Chat, Notifications
- ✅ Automatic reconnection
- ✅ Groups for targeted messaging

### **Workers (Background Services)**
- ✅ Runs continuously in background
- ✅ Perfect for scheduled tasks
- ✅ Graceful shutdown support
- ✅ Must use SemaphoreSlim to prevent overlap
- ✅ Must create AsyncScope for Scoped services

### **Use Cases**
- **SignalR**: Live chat, notifications, real-time updates
- **Workers**: Scheduled notifications, cleanup, batch processing, expiration

---

**Created**: 2025-11-05  
**Project**: ASP_LorKingDom_PRN222  
**Framework**: ASP.NET Core 8
