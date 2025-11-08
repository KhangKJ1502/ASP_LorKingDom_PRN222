# ❓ SignalR & Worker - Troubleshooting & FAQs

## 📑 Mục lục
1. [Common Problems & Solutions](#common-problems--solutions)
2. [Debugging Tips](#debugging-tips)
3. [Performance Optimization](#performance-optimization)
4. [Frequently Asked Questions](#frequently-asked-questions)

---

## Common Problems & Solutions

### SignalR Issues

#### **Problem 1: Connection Fails - CORS Error**

**Error Message:**
```
Cross-Origin Request Blocked: ... Reason: CORS header 'Access-Control-Allow-Credentials' missing
```

**Root Cause:**
- CORS policy không allow credentials
- SignalR uses cookies/authentication

**Solution:**
```csharp
// ✅ CORRECT
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy
            .WithOrigins("https://example.com", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // ✅ MUST include this
    });
});

app.UseCors("SignalRPolicy");

// ✅ And map hub
app.MapHub<ChatHub>("/chatHub");
```

**Prevention:**
```javascript
// Frontend: Ensure credentials are sent
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
        withCredentials: true // ✅ Include credentials
    })
    .build();
```

---

#### **Problem 2: Client Never Receives Messages**

**Symptoms:**
- `connection.on()` listeners never fired
- Messages sent from server but client doesn't receive

**Root Causes:**
| Cause | Check |
|-------|-------|
| Not in correct group | Verify group name in `Groups.AddToGroupAsync()` |
| Wrong client method name | Ensure `SendAsync("methodName")` matches `connection.on("methodName")` |
| Connection not established | Check `connection.state === signalR.HubConnectionState.Connected` |
| Listener not registered | Call `connection.on()` BEFORE `connection.start()` |
| Proxy/Firewall blocking | Check network in DevTools → Network tab |

**Solution:**
```javascript
// 1. Register listeners BEFORE connecting
connection.on("receiveMessage", (message) => {
    console.log("Message:", message);
});

// 2. Check connection state
connection.start()
    .then(() => {
        console.log("State:", connection.state); // Should be Connected (1)
        console.log("Connection ID:", connection.connectionId);
    });

// 3. Debug: Log all incoming messages
connection.onreceive = (data) => {
    console.log("📨 Raw message received:", data);
};
```

**Server-side Debug:**
```csharp
// Add logging to Hub method
public async Task SendMessage(SendMessageDto dto)
{
    _logger.LogInformation(
        "📩 Message received: From {From} To {To}: {Content}",
        dto.SenderId, dto.RecipientId, dto.Content);

    // Check groups
    var allConnections = _logger.LogInformation(
        "Current connections: {Count}",
        Context.ConnectionId);

    // Send with explicit logging
    _logger.LogInformation(
        "📤 Sending to group: user:{UserId}",
        dto.RecipientId);

    await Clients.Group($"user:{dto.RecipientId}")
        .SendAsync("receiveMessage", dto);
}
```

---

#### **Problem 3: Connection Keeps Reconnecting**

**Symptoms:**
```
Connection established
... (after 5 seconds)
Connection lost
Connection re-establishing
Connection established
... repeat ...
```

**Root Causes:**
| Cause | Signs |
|-------|-------|
| Server timeout | "Connection lost" every ~30 seconds |
| Client-side errors | Exception in `connection.on()` handler |
| Network instability | Intermittent reconnects |
| Load balancer sticky sessions | Reconnects after each request |

**Solution:**
```javascript
// 1. Handle reconnection events
connection.onreconnecting((error) => {
    console.warn("⚠️ Connection lost. Reconnecting...", error?.message);
    // Show UI indicator
    document.getElementById("connectionStatus").textContent = "Reconnecting...";
});

connection.onreconnected((connectionId) => {
    console.log("✅ Reconnected with ID:", connectionId);
    // Update UI
    document.getElementById("connectionStatus").textContent = "Connected";
});

connection.onclose((error) => {
    console.error("❌ Connection closed permanently:", error?.message);
    // Show error UI
    document.getElementById("connectionStatus").textContent = "Disconnected";
});

// 2. Increase keep-alive timeout
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect([
        0,    // 0ms
        2000, // 2s
        5000, // 5s
        10000 // 10s
    ]) // ✅ Custom reconnect delays
    .build();

// 3. Fix error handlers
connection.on("receiveMessage", (message) => {
    try {
        // Your code
    } catch (error) {
        console.error("❌ Error in message handler:", error);
        // Don't let error break the connection
    }
});
```

**Server-side:**
```csharp
// Increase keep-alive
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});
```

---

#### **Problem 4: High Latency / Slow Messages**

**Symptoms:**
- Message takes 3+ seconds to arrive
- UI feels unresponsive

**Root Causes:**
| Cause | Fix |
|-------|-----|
| Large message payload | Compress/batch messages |
| Server slow | Profile server code |
| Network latency | Use CDN for static files |
| UI rendering slow | Use virtual scrolling |

**Solution:**
```javascript
// 1. Compress messages
const message = {
    id: 1,
    c: "Hello", // ✅ short property name
    s: "2025-11-05", // ✅ short date
};

// 2. Batch messages (send multiple at once)
const messageBatch = [];
function addToBatch(message) {
    messageBatch.push(message);
    if (messageBatch.length >= 10) {
        sendBatch();
    }
}

async function sendBatch() {
    await connection.invoke("SendBatch", messageBatch);
    messageBatch.length = 0; // Clear
}

// 3. Virtual scrolling (not load all messages at once)
const observer = new IntersectionObserver(entries => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            loadMoreMessages();
        }
    });
});
observer.observe(document.getElementById("loadMore"));
```

**Server-side:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
    options.StreamBufferCapacity = 16; // Reduce buffer
})
.AddMessagePackProtocol(); // ✅ Use binary protocol instead of JSON
```

---

### Worker Issues

#### **Problem 1: Worker Never Executes**

**Symptoms:**
- No logs from worker
- Job never runs

**Root Causes:**
| Cause | Check |
|-------|-------|
| Service not registered | Check `AddHostedService<>()` in Program.cs |
| App shutdown immediately | Check if exception in startup |
| StoppingToken triggered | Check app lifecycle |

**Solution:**
```csharp
// ✅ In Program.cs
builder.Services.AddHostedService<NotificationWorkerService>();

// ✅ Add comprehensive logging
var app = builder.Build();

app.Logger.LogInformation("🚀 Application started");

// ✅ Keep app running (don't exit immediately)
app.Run(); // This blocks until Ctrl+C or shutdown signal
```

**Debug:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("✅ ExecuteAsync started"); // Should see this immediately

    // Add startup log
    _logger.LogInformation("🔄 Entering main loop");

    // ...
}
```

**Check:**
```bash
# Check if app is running
Get-Process dotnet

# View logs
tail -f logs/app.log | grep NotificationWorkerService
```

---

#### **Problem 2: Execution Overlaps - Data Corruption**

**Symptoms:**
- Duplicate records in database
- Data inconsistency
- Warnings in logs: "Previous execution still running"

**Root Cause:**
- Execution takes too long (> interval)
- No semaphore lock

**Solution:**
```csharp
// ✅ Always use SemaphoreSlim
private readonly SemaphoreSlim _gate = new(1, 1);

private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    // Non-blocking wait (0ms timeout)
    if (!await _gate.WaitAsync(0, ct))
    {
        _logger.LogWarning("⚠️ Previous execution still running; skipping");
        return; // Skip this tick
    }

    try
    {
        await ProcessAsync(ct);
    }
    finally
    {
        _gate.Release(); // ✅ ALWAYS release
    }
}
```

**Prevent:**
```csharp
// 1. Reduce interval if execution completes too slowly
private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Increase to 5 min

// 2. Optimize database query
var due = await _context.Notifications
    .Where(n => !n.IsSent && n.ScheduledAt <= DateTime.UtcNow)
    .Take(1000) // ✅ Limit to prevent timeout
    .ToListAsync();

// 3. Batch process
foreach (var batch in due.Batch(size: 100))
{
    await ProcessBatchAsync(batch);
}
```

---

#### **Problem 3: Worker Crashes Silently**

**Symptoms:**
- Worker stops working
- No log entries (or very few)
- Job doesn't execute anymore

**Root Cause:**
- Unhandled exception breaks the loop

**Solution:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var timer = new PeriodicTimer(_interval);

    try
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // ✅ Always wrap in try-catch
            await SafeProcessOnceAsync(stoppingToken);
        }
    }
    catch (OperationCanceledException)
    {
        // ✅ Expected on shutdown
    }
    catch (Exception ex)
    {
        // ✅ Log but don't crash
        _logger.LogCritical(ex, "💥 Worker crashed");
        // Optional: notify admin
    }
    finally
    {
        timer.Dispose();
    }
}
```

**Debug:**
```csharp
private async Task SafeProcessOnceAsync(CancellationToken ct)
{
    try
    {
        _logger.LogDebug("🔄 Execution started at {Time}", DateTime.UtcNow);
        // ... do work ...
        _logger.LogDebug("✅ Execution completed");
    }
    catch (Exception ex)
    {
        // Detailed error logging
        _logger.LogError(ex, "❌ Error type: {ExceptionType}, Message: {Message}",
            ex.GetType().Name, ex.Message);

        // Nested exceptions
        var innerEx = ex.InnerException;
        while (innerEx != null)
        {
            _logger.LogError("  └─ {Message}", innerEx.Message);
            innerEx = innerEx.InnerException;
        }
    }
}
```

---

#### **Problem 4: Worker Can't Access Database**

**Symptoms:**
```
DbUpdateException: Unable to connect to database
or
NullReferenceException: IOrderRepository is null
```

**Root Causes:**
| Cause | Fix |
|-------|-----|
| Scoped service not in scope | Create `CreateAsyncScope()` |
| Service not registered in DI | Check `AddDAL()`, `AddBLL()` in Program.cs |
| Connection string missing | Check `appsettings.json` |

**Solution:**
```csharp
// ✅ WRONG
public class Worker : BackgroundService
{
    private readonly IOrderService _service; // ❌ Scoped injected into Singleton

    public Worker(IOrderService service) // ❌ This fails
    {
        _service = service;
    }
}

// ✅ CORRECT
public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider; // ✅ Inject ServiceProvider

    public Worker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        // Create scope for this execution
        await using var scope = _serviceProvider.CreateAsyncScope();
        
        // Get scoped service inside scope
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        
        // Use service
        await service.ProcessAsync();
    }
}
```

---

## Debugging Tips

### 1. **Enable Detailed Logging**

```csharp
// Program.cs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Optional: Add file logging
builder.Logging.AddFile("logs/app-{Date}.log");
```

### 2. **SignalR Client Debugging**

```javascript
// Enable SignalR client logging
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .configureLogging(signalR.LogLevel.Debug) // ✅ Enable debug
    .build();

// Monit console for SignalR messages:
// [signalr] Information: Connected...
// [signalr] Debug: Sending message...
```

### 3. **Check Connection State**

```javascript
function getConnectionStatus() {
    const states = {
        0: "Connecting",
        1: "Connected",
        2: "Reconnecting",
        3: "Disconnecting",
        4: "Disconnected",
        5: "Failed"
    };
    
    console.log("Current state:", states[connection.state]);
    console.log("Connection ID:", connection.connectionId);
    console.log("Server URL:", connection.baseUrl);
}

// Call periodically
setInterval(getConnectionStatus, 5000);
```

### 4. **Test Hub Methods**

```csharp
// Add test endpoint
[HttpGet("test-hub")]
public async Task<IActionResult> TestHub()
{
    // Send test message to all
    await _hubContext.Clients.All.SendAsync("testMessage", new
    {
        message = "Hello from server",
        timestamp = DateTime.UtcNow
    });

    return Ok("Message sent");
}
```

```javascript
// Test endpoint
fetch("/api/test-hub")
    .then(r => r.json())
    .then(data => console.log("Response:", data));

// Listen for test message
connection.on("testMessage", (data) => {
    console.log("✅ Hub working! Received:", data);
});
```

---

## Performance Optimization

### 1. **SignalR Optimization**

```csharp
// Use MessagePack (binary format)
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// Configure limits
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// Scale SignalR (for multiple servers)
builder.Services.AddSignalR()
    .AddRedis("localhost:6379"); // Use Redis backplane
```

### 2. **Worker Optimization**

```csharp
// 1. Use batching
private async Task ProcessAsync()
{
    const int batchSize = 500;
    
    var pending = await _repo.GetPendingAsync(batchSize);
    
    foreach (var batch in pending.Batch(batchSize: 50))
    {
        await _repo.ProcessBatchAsync(batch); // Faster than individual
    }
}

// 2. Parallel processing (carefully)
private async Task ProcessAsync()
{
    var items = await _repo.GetPendingAsync();
    
    // Use Partitioner for thread-safe processing
    Parallel.ForEach(
        items.Partition(Environment.ProcessorCount),
        new ParallelOptions { MaxDegreeOfParallelism = 4 },
        async item => await ProcessItemAsync(item));
}

// 3. Adjust interval
private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Not too frequent
```

---

## Frequently Asked Questions

### **Q1: Can multiple workers access the same database without conflicts?**

**A:** Yes, but needs precautions:

```csharp
// ✅ Each worker should have its own semaphore
private readonly SemaphoreSlim _gate = new(1, 1);

// ✅ Use database-level locks if needed
using var transaction = await _context.Database.BeginTransactionAsync(
    System.Data.IsolationLevel.Serializable); // Highest isolation

// ✅ Alternatively: Partition data
// Worker 1: Process orders 0-50000
// Worker 2: Process orders 50001-100000
```

---

### **Q2: How to gracefully shutdown a SignalR hub connection?**

**A:**

```javascript
// Frontend
async function closeConnection() {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.stop();
            console.log("✅ Disconnected gracefully");
        } catch (err) {
            console.error("❌ Error disconnecting:", err);
        }
    }
}

// Call on page unload
window.addEventListener("beforeunload", closeConnection);
```

---

### **Q3: How to handle SignalR message ordering?**

**A:**

```javascript
// Add message IDs to ensure ordering
let messageId = 0;

function sendMessage(content) {
    connection.invoke("SendMessage", {
        id: messageId++,
        content: content,
        timestamp: Date.now()
    });
}

// Sort on receive
const messages = [];
connection.on("receiveMessage", (message) => {
    messages.push(message);
    messages.sort((a, b) => a.id - b.id); // Sort by ID
    renderMessages(messages);
});
```

---

### **Q4: How often should workers run?**

**A:** Depends on use case:

| Use Case | Interval |
|----------|----------|
| Notifications | 1-5 minutes |
| Order status check | 5-15 minutes |
| Cleanup/archival | 1 hour |
| Analytics | 1-6 hours |
| **Rule of thumb** | Balance between latency & load |

---

### **Q5: Can I stop/start workers dynamically?**

**A:**

```csharp
// ✅ Using IHostApplicationLifetime
public class DynamicWorker : BackgroundService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private CancellationTokenSource _cts = new();

    public DynamicWorker(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
    }

    public void StopWorker() => _cts.Cancel();
    public void StartWorker() => _cts = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var linkedCts = CancellationTokenSource
            .CreateLinkedTokenSource(stoppingToken, _cts.Token);

        while (!linkedCts.Token.IsCancellationRequested)
        {
            // Work...
        }
    }
}
```

---

### **Q6: How to monitor worker health?**

**A:**

```csharp
public class WorkerHealthCheck : IHealthCheck
{
    private DateTime _lastExecution = DateTime.UtcNow;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var timeSinceLastExecution = DateTime.UtcNow - _lastExecution;

        if (timeSinceLastExecution > TimeSpan.FromMinutes(10))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Worker not executing"));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}

// Register
builder.Services.AddHealthChecks()
    .AddCheck<WorkerHealthCheck>("worker-health");
```

---

**Last Updated**: 2025-11-05  
**Project**: ASP_LorKingDom_PRN222
