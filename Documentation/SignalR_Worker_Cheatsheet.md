# ⚡ SignalR & Worker - Cheatsheet

## 🚀 Quick Reference

### **SignalR - 30 Second Setup**

```csharp
// 1. Program.cs
builder.Services.AddSignalR();
app.MapHub<ChatHub>("/chatHub");

// 2. ChatHub.cs
public class ChatHub : Hub
{
    public async Task SendMessage(string content)
    {
        await Clients.All.SendAsync("receiveMessage", content);
    }
}

// 3. JavaScript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("receiveMessage", (msg) => console.log(msg));

connection.start()
    .catch(err => console.error(err));

// 4. Send
connection.invoke("SendMessage", "Hello");
```

---

### **Worker - 30 Second Setup**

```csharp
// 1. Create Worker
public class MyWorkerService : BackgroundService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!await _gate.WaitAsync(0)) return;
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IMyService>();
                await service.ProcessAsync();
            }
            finally { _gate.Release(); }
        }
    }
}

// 2. Program.cs
builder.Services.AddHostedService<MyWorkerService>();
```

---

## 📡 SignalR Methods - Hub to Client

```javascript
// Send to ALL clients
await Clients.All.SendAsync("methodName", data);

// Send to OTHERS (not sender)
await Clients.Others.SendAsync("methodName", data);

// Send to CALLER only
await Clients.Caller.SendAsync("methodName", data);

// Send to SPECIFIC USER (by group)
await Clients.Group($"user:{userId}").SendAsync("methodName", data);

// Send to MULTIPLE GROUPS
await Clients.Groups("staff", "admin").SendAsync("methodName", data);

// Exclude specific user
await Clients.AllExcept(connectionId).SendAsync("methodName", data);
```

---

## 👥 Groups - User Management

```csharp
// Add to group
await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

// Remove from group
await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");

// Remove all groups
await Groups.RemoveFromAllGroupsAsync(Context.ConnectionId);

// Send to group
await Clients.Group($"user:{userId}").SendAsync("message", "hello");
```

---

## 🔌 Lifecycle Methods

```csharp
public class ChatHub : Hub
{
    // When client connects
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    // When client disconnects
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        await Groups.RemoveFromAllGroupsAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

---

## 🎫 Client-side Methods

```javascript
// Connect
await connection.start();

// Disconnect
await connection.stop();

// Listen for message
connection.on("messageMethod", (data) => { });

// Send to server
await connection.invoke("ServerMethod", param1, param2);

// Handle reconnection
connection.onreconnecting((error) => {
    console.log("Reconnecting...", error);
});

connection.onreconnected((connectionId) => {
    console.log("Reconnected!", connectionId);
});
```

---

## ⚙️ Configuration

```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
})
.AddMessagePackProtocol(); // Binary protocol
```

---

## 🔐 Authentication & Authorization

```csharp
// Require authentication
[Authorize]
public class ChatHub : Hub { }

// Require specific role
public async Task AdminOnly()
{
    var user = Context.User;
    if (!user?.IsInRole("Admin") ?? true)
    {
        throw new HubException("Admin required");
    }
}

// Get current user info
var userId = Context.User?.FindFirst("nameid")?.Value;
var email = Context.User?.FindFirst("email")?.Value;
```

---

## 🔄 Worker Patterns

### Pattern 1: Simple Loop

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            await DoWork();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error");
        }
    }
}
```

### Pattern 2: PeriodicTimer (Recommended)

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        try { await DoWork(); }
        catch (Exception ex) { _logger.LogError(ex, "Error"); }
    }
}
```

### Pattern 3: With Semaphore

```csharp
private readonly SemaphoreSlim _gate = new(1, 1);

private async Task SafeProcessAsync()
{
    if (!await _gate.WaitAsync(0)) return; // Skip if running
    try { await DoWork(); }
    finally { _gate.Release(); }
}
```

---

## 📊 Error Handling

```csharp
// Hub - Catch errors
public async Task SendMessage(string msg)
{
    try
    {
        if (string.IsNullOrEmpty(msg))
            throw new HubException("Message cannot be empty");
        // Process...
    }
    catch (HubException ex)
    {
        await Clients.Caller.SendAsync("error", ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        await Clients.Caller.SendAsync("error", "Server error");
    }
}

// Worker - Graceful degradation
try
{
    await DoWork();
}
catch (DbException ex) when (IsTransientError(ex))
{
    _logger.LogWarning("Transient error, will retry next cycle");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Fatal error");
}
```

---

## 🧪 Testing

```csharp
// Test Hub method
[Fact]
public async Task SendMessage_BroadcastsToAll()
{
    var hub = new ChatHub(_mockService.Object, _mockLogger.Object);
    var mockClients = new Mock<IHubCallerClients>();
    hub.Clients = mockClients.Object;

    await hub.SendMessage("Hello");

    mockClients.Verify(
        x => x.All.SendAsync("receiveMessage", "Hello", default),
        Times.Once());
}

// Test Worker
[Fact]
public async Task Worker_ProcessesItems()
{
    var worker = new TestWorkerService(_mockService.Object);
    var cts = new CancellationTokenSource();
    
    var task = worker.StartAsync(cts.Token);
    await Task.Delay(100); // Let it run
    cts.Cancel();
    
    await task;
    
    _mockService.Verify(x => x.ProcessAsync(), Times.Once());
}
```

---

## 📝 Logging Patterns

```csharp
// SignalR
_logger.LogInformation("✅ User {UserId} connected", userId);
_logger.LogWarning("⚠️ Previous execution running");
_logger.LogError(ex, "❌ Error processing");
_logger.LogDebug("🔍 Sending to group {Group}", groupName);

// Worker
_logger.LogInformation("🚀 Worker starting");
_logger.LogDebug("▶️ Execution #{Count}", executionCount);
_logger.LogInformation("📊 Processed {Count} items", count);
_logger.LogDebug("✅ Completed in {Ms}ms", stopwatch.ElapsedMilliseconds);
```

---

## 🚨 Troubleshooting Quick Fixes

| Problem | Fix |
|---------|-----|
| CORS error | Add `.AllowCredentials()` |
| Messages not received | Check group name format |
| Connection keeps reconnecting | Check server logs for errors |
| Worker not executing | Check `AddHostedService<>()` in Program.cs |
| Data corruption | Add SemaphoreSlim lock |
| Slow messages | Use MessagePack protocol |
| High memory | Implement message batching |

---

## 🔗 Key Classes & Interfaces

```csharp
// SignalR
Hub // Base class for hubs
HubContext<T> // Send messages from anywhere
IHubContext<T> // Injected version
IAsyncResult // For async methods

// Workers
BackgroundService // Base class for workers
IHostedService // Interface (implement for custom)
IHostApplicationLifetime // App lifecycle

// DI
IServiceProvider // Get services
IServiceScope // Scoped container
CreateAsyncScope() // For workers
```

---

## 💾 Dependency Injection

```csharp
// Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<ILogger<ChatHub>>();
builder.Services.AddHostedService<MyWorker>();

// In Hub
public ChatHub(IChatService service, ILogger<ChatHub> logger)
{
    _service = service;
    _logger = logger;
}

// In Worker
await using var scope = _serviceProvider.CreateAsyncScope();
var service = scope.ServiceProvider.GetRequiredService<IMyService>();
```

---

## 🎯 Common Patterns

### Pattern: Broadcast Notification

```csharp
await Clients.All.SendAsync("notification", new
{
    title = "Alert",
    message = "Something happened",
    timestamp = DateTime.UtcNow
});
```

### Pattern: User-specific Update

```csharp
await Clients.Group($"user:{userId}").SendAsync("update", data);
```

### Pattern: Typing Indicator

```csharp
connection.on("userTyping", (data) => {
    console.log(data.userId, "is typing...");
    setTimeout(() => hideTyping(), 3000);
});
```

### Pattern: Presence Tracking

```csharp
public override async Task OnConnectedAsync()
{
    await Clients.Others.SendAsync("userOnline", userId);
}

public override async Task OnDisconnectedAsync(Exception? ex)
{
    await Clients.Others.SendAsync("userOffline", userId);
}
```

---

## 📈 Performance Tips

| Optimization | Effect |
|--------------|--------|
| Use MessagePack | -20% bandwidth |
| Batch messages | -50% requests |
| Increase keep-alive | -Network timeouts |
| Use Groups | -CPU load |
| Semaphore lock | -Data corruption |
| Async/await | -Thread pool |

---

## 🗂️ File Structure

```
WebUI/
├── ChatHubs/
│   └── ChatHub.cs ..................... SignalR hub
├── Workers/
│   ├── NotificationWorkerService.cs ... Notification worker
│   └── PromotionWorkerService.cs ..... Promotion worker
└── Program.cs ........................ Configuration
```

---

## 📚 One-liners

```csharp
// Add to group
await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

// Send to user
await Clients.Group($"user:{userId}").SendAsync("method", data);

// Create worker scope
await using var scope = _serviceProvider.CreateAsyncScope();

// Get scoped service
var service = scope.ServiceProvider.GetRequiredService<IService>();

// Prevent overlap
if (!await _gate.WaitAsync(0)) return;

// Wait for timer tick
while (await timer.WaitForNextTickAsync(token)) { }
```

---

**Last Updated**: 2025-11-05  
**For Full Details**: See SignalR_Worker_Guide.md
