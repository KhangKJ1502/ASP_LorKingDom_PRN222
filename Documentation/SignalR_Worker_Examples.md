# 🔥 SignalR & Worker - Code Examples & Practical Usage

## 📑 Mục lục
1. [SignalR Examples](#signalr-examples)
2. [Worker Examples](#worker-examples)
3. [Integration Examples](#integration-examples)
4. [Real-world Scenarios](#real-world-scenarios)

---

## SignalR Examples

### Example 1: Basic Chat - Send & Receive

#### **Backend - ChatHub**

```csharp
using Microsoft.AspNetCore.SignalR;
using BLL.Interfaces;
using BLL.DTOs;

namespace WebUI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // 🔴 Server method: Client gửi message
        public async Task SendMessage(SendMessageDto dto)
        {
            try
            {
                // 1. Validate
                if (string.IsNullOrWhiteSpace(dto.Content))
                {
                    await Clients.Caller.SendAsync("error", "Message cannot be empty");
                    return;
                }

                // 2. Get current user
                var http = Context.GetHttpContext();
                var senderId = http?.Request.Query["userId"].ToString();

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    await Clients.Caller.SendAsync("error", "User not authenticated");
                    return;
                }

                // 3. Save to database
                var message = new MessageDto
                {
                    SenderId = senderId,
                    RecipientId = dto.RecipientId,
                    ConversationId = dto.ConversationId,
                    Content = dto.Content,
                    SentAt = DateTime.UtcNow
                };

                var savedMessage = await _chatService.SaveMessageAsync(message);

                // 4. Send to recipient
                await Clients.Group($"user:{dto.RecipientId}")
                    .SendAsync("receiveMessage", new
                    {
                        id = savedMessage.Id,
                        conversationId = savedMessage.ConversationId,
                        senderId = senderId,
                        senderName = dto.SenderName,
                        content = savedMessage.Content,
                        sentAt = savedMessage.SentAt
                    });

                // 5. Confirm to sender
                await Clients.Caller.SendAsync("messageSent", new
                {
                    id = savedMessage.Id,
                    status = "delivered"
                });

                _logger.LogInformation(
                    "Message sent from {SenderId} to {RecipientId}",
                    senderId, dto.RecipientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("error", "Failed to send message");
            }
        }

        // 🔴 Server method: Mark message as read
        public async Task MarkAsRead(int messageId, int conversationId)
        {
            try
            {
                var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

                // Save read status
                await _chatService.MarkAsReadAsync(messageId, userId);

                // Notify sender
                await Clients.All.SendAsync("messageRead", new
                {
                    messageId = messageId,
                    readBy = userId,
                    readAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
            }
        }

        // 🔴 Server method: Typing indicator
        public async Task UserTyping(int conversationId)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            // Broadcast to others (not sender)
            await Clients.Others.SendAsync("userTyping", new
            {
                userId = userId,
                conversationId = conversationId
            });
        }

        // 🟢 Lifecycle: Connection established
        public override async Task OnConnectedAsync()
        {
            try
            {
                var http = Context.GetHttpContext();
                var userId = http?.Request.Query["userId"].ToString();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Connection rejected: missing userId");
                    Context.Abort();
                    return;
                }

                // Add to user group
                await Groups.AddToGroupAsync(
                    Context.ConnectionId, $"user:{userId}");

                // Save connection to DB
                await _chatService.UserConnectedAsync(
                    userId,
                    Context.ConnectionId,
                    DateTime.UtcNow);

                // Notify others
                await Clients.Others.SendAsync("userOnline", userId);

                _logger.LogInformation("User {UserId} connected", userId);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
                throw;
            }
        }

        // 🟢 Lifecycle: Connection closed
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var http = Context.GetHttpContext();
                var userId = http?.Request.Query["userId"].ToString();

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    // Remove from user group
                    await Groups.RemoveFromGroupAsync(
                        Context.ConnectionId, $"user:{userId}");

                    // Update DB
                    await _chatService.UserDisconnectedAsync(
                        userId, DateTime.UtcNow);

                    // Notify others
                    await Clients.Others.SendAsync("userOffline", userId);

                    _logger.LogInformation("User {UserId} disconnected", userId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
            }
        }
    }
}
```

#### **Frontend - JavaScript**

```html
<!DOCTYPE html>
<html>
<head>
    <title>Chat Application</title>
    <script src="~/lib/signalr/signalr.js"></script>
</head>
<body>
    <div id="messageList"></div>
    <textarea id="messageInput" placeholder="Type your message..."></textarea>
    <button onclick="sendMessage()">Send</button>

    <script>
        const userId = "123"; // Current user ID
        const recipientId = "456"; // Target user ID
        const conversationId = 1;

        // ===== 1. Create Connection =====
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub?userId=" + userId)
            .withAutomaticReconnect() // Auto reconnect on disconnect
            .build();

        // ===== 2. Listen for messages =====
        connection.on("receiveMessage", (message) => {
            console.log("💬 Message received:", message);
            displayMessage(message);
        });

        // ===== 3. Listen for read receipts =====
        connection.on("messageRead", (data) => {
            console.log("👁️ Message read:", data);
            markMessageAsRead(data.messageId);
        });

        // ===== 4. Listen for typing indicator =====
        connection.on("userTyping", (data) => {
            console.log("⌨️ User typing:", data.userId);
            showTypingIndicator(data.userId);
        });

        // ===== 5. Listen for user status =====
        connection.on("userOnline", (userId) => {
            console.log("✅ User online:", userId);
            updateUserStatus(userId, "online");
        });

        connection.on("userOffline", (userId) => {
            console.log("❌ User offline:", userId);
            updateUserStatus(userId, "offline");
        });

        // ===== 6. Listen for errors =====
        connection.on("error", (message) => {
            console.error("❌ Error:", message);
            alert(message);
        });

        // ===== 7. Connect to server =====
        connection.start()
            .then(() => {
                console.log("✅ Connected to Chat Hub");
                loadConversationHistory();
            })
            .catch(err => {
                console.error("❌ Connection error:", err);
                // Retry logic
                setTimeout(() => connection.start(), 3000);
            });

        // ===== 8. Send message =====
        async function sendMessage() {
            const content = document.getElementById("messageInput").value;

            if (!content.trim()) {
                alert("Message cannot be empty");
                return;
            }

            try {
                // Invoke server method
                await connection.invoke("SendMessage", {
                    conversationId: conversationId,
                    recipientId: recipientId,
                    content: content,
                    senderName: "Me"
                });

                // Clear input
                document.getElementById("messageInput").value = "";
            }
            catch (err) {
                console.error("❌ Error sending message:", err);
                alert("Failed to send message");
            }
        }

        // ===== 9. Display message =====
        function displayMessage(message) {
            const messageList = document.getElementById("messageList");
            const messageDiv = document.createElement("div");
            messageDiv.className = "message";
            messageDiv.id = "msg-" + message.id;
            messageDiv.innerHTML = `
                <strong>${message.senderName}:</strong>
                <p>${message.content}</p>
                <small>${new Date(message.sentAt).toLocaleTimeString()}</small>
            `;
            messageList.appendChild(messageDiv);

            // Auto scroll to bottom
            messageList.scrollTop = messageList.scrollHeight;

            // Mark as read
            setTimeout(() => {
                connection.invoke("MarkAsRead", message.id, message.conversationId);
            }, 1000);
        }

        // ===== 10. Typing indicator =====
        let typingTimer;
        document.getElementById("messageInput").addEventListener("keyup", () => {
            clearTimeout(typingTimer);

            connection.invoke("UserTyping", conversationId);

            typingTimer = setTimeout(() => {
                // Stop typing after 1 second of inactivity
            }, 1000);
        });

        // Show typing indicator UI
        function showTypingIndicator(userId) {
            const messageList = document.getElementById("messageList");
            const typingDiv = document.getElementById("typing-" + userId) 
                || document.createElement("div");
            
            typingDiv.id = "typing-" + userId;
            typingDiv.className = "typing-indicator";
            typingDiv.textContent = userId + " is typing...";
            
            if (!typingDiv.parentElement) {
                messageList.appendChild(typingDiv);
            }

            // Remove after 3 seconds
            setTimeout(() => typingDiv.remove(), 3000);
        }

        // ===== 11. Load conversation history =====
        function loadConversationHistory() {
            // Fetch from API
            fetch(`/api/conversations/${conversationId}/messages`)
                .then(r => r.json())
                .then(messages => {
                    messages.forEach(msg => displayMessage(msg));
                });
        }
    </script>
</body>
</html>
```

---

### Example 2: Broadcast Notification

#### **Backend**

```csharp
// Broadcast notification to all connected clients
public async Task BroadcastNotification(NotificationDto notification)
{
    // Send to everyone
    await Clients.All.SendAsync("receiveNotification", new
    {
        id = notification.Id,
        title = notification.Title,
        message = notification.Message,
        type = notification.Type, // Info, Warning, Error, Success
        timestamp = DateTime.UtcNow
    });
}

// Broadcast to specific user
public async Task NotifyUser(string userId, string message)
{
    await Clients.Group($"user:{userId}")
        .SendAsync("receiveNotification", new
        {
            message = message,
            timestamp = DateTime.UtcNow
        });
}

// Broadcast to staff members only
public async Task NotifyStaff(string message)
{
    await Clients.Group("staff")
        .SendAsync("receiveNotification", new
        {
            message = message,
            audience = "staff",
            timestamp = DateTime.UtcNow
        });
}
```

#### **Frontend**

```javascript
// Listen for notifications
connection.on("receiveNotification", (notification) => {
    console.log("🔔 Notification:", notification);
    
    // Show toast notification
    showToast(notification.message, notification.type);
    
    // Play sound
    playNotificationSound();
    
    // Update badge
    updateNotificationBadge();
});

function showToast(message, type = "info") {
    const toast = document.createElement("div");
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);

    // Auto remove after 5 seconds
    setTimeout(() => toast.remove(), 5000);
}
```

---

## Worker Examples

### Example 1: Notification Worker - Detailed

```csharp
using BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WebUI.Workers
{
    /// <summary>
    /// Background service: Gửi notifications đã đến hạn
    /// Chạy mỗi 1 phút
    /// </summary>
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Configuration
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
        private readonly SemaphoreSlim _gate = new(1, 1); // Prevent overlap
        private int _executionCount = 0;

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "🚀 NotificationWorkerService starting. Interval: {Interval} minutes",
                _interval.TotalMinutes);

            // Random delay (0-5 seconds) to prevent thundering herd
            var startupJitterMs = Random.Shared.Next(0, 5000);
            _logger.LogDebug("⏳ Startup jitter: {Jitter}ms", startupJitterMs);

            try
            {
                await Task.Delay(startupJitterMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ Startup delayed cancelled");
                return;
            }

            // Create periodic timer
            using var timer = new PeriodicTimer(_interval);

            try
            {
                _logger.LogInformation("⏰ Starting periodic timer loop");

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    _executionCount++;
                    _logger.LogDebug("▶️ Execution #{Count} at {Time}",
                        _executionCount,
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Worker cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "💥 NotificationWorkerService crashed unexpectedly");
            }
            finally
            {
                _logger.LogInformation(
                    "🏁 NotificationWorkerService stopping. Total executions: {Count}",
                    _executionCount);
            }
        }

        private async Task SafeProcessOnceAsync(CancellationToken ct)
        {
            // Check if previous execution is still running
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogWarning(
                    "⚠️ Previous execution still running; skipping this tick");
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                await ProcessDueNotificationsAsync(ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ Processing cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in SafeProcessOnceAsync");
            }
            finally
            {
                sw.Stop();
                _gate.Release();

                _logger.LogDebug(
                    "✅ Execution #{Exec} finished in {ElapsedMs}ms",
                    _executionCount,
                    sw.ElapsedMilliseconds);
            }
        }

        private async Task ProcessDueNotificationsAsync(CancellationToken ct)
        {
            // Create scope for Scoped services
            await using var scope = _serviceProvider.CreateAsyncScope();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<INotificationService>();
            var chatHubContext = scope.ServiceProvider
                .GetRequiredService<IHubContext<ChatHub>>();

            try
            {
                _logger.LogDebug("🔍 Querying due notifications...");

                // Get all notifications that need to be sent
                var count = await notificationService.DispatchDueAsync();

                if (count > 0)
                {
                    _logger.LogInformation(
                        "📤 Processed {Count} due notification(s) at {UtcTime}",
                        count,
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                    // Optional: Broadcast to connected clients
                    await chatHubContext.Clients.All
                        .SendAsync("notificationDispatched", new
                        {
                            count = count,
                            dispatchedAt = DateTime.UtcNow
                        });
                }
                else
                {
                    _logger.LogDebug("✅ No due notifications at {UtcTime}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
            catch (DbException ex) when (IsTransientDbError(ex))
            {
                _logger.LogWarning(ex,
                    "⚠️ Transient database error; will retry next cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error occurred while processing due notifications");
            }
        }

        private bool IsTransientDbError(DbException ex)
        {
            // SQL Server transient error codes
            return ex.Number == -2 || // Timeout
                   ex.Number == 64 || // SQL Server network timeout
                   ex.Number == 233; // Resource temporarily unavailable
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 NotificationWorkerService is stopping gracefully");
            return base.StopAsync(stoppingToken);
        }
    }
}
```

### Example 2: Custom Worker - Order Status Checker

```csharp
namespace WebUI.Workers
{
    /// <summary>
    /// Background service: Kiểm tra orders quá 24h chưa thanh toán
    /// Nếu vượt quá, tự động cancel order
    /// </summary>
    public class OrderExpirationWorkerService : BackgroundService
    {
        private readonly ILogger<OrderExpirationWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Check hourly
        private readonly SemaphoreSlim _gate = new(1, 1);

        public OrderExpirationWorkerService(
            ILogger<OrderExpirationWorkerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "🚀 OrderExpirationWorkerService starting");

            // Random delay
            await Task.Delay(Random.Shared.Next(0, 5000), stoppingToken);

            using var timer = new PeriodicTimer(_interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SafeProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "💥 OrderExpirationWorkerService crashed");
            }
            finally
            {
                _logger.LogInformation("🏁 OrderExpirationWorkerService stopping");
            }
        }

        private async Task SafeProcessOnceAsync(CancellationToken ct)
        {
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogWarning("⚠️ Previous check still running");
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                await CheckExpiredOrdersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking expired orders");
            }
            finally
            {
                sw.Stop();
                _gate.Release();
                _logger.LogDebug("✅ Check finished in {Ms}ms", sw.ElapsedMilliseconds);
            }
        }

        private async Task CheckExpiredOrdersAsync(CancellationToken ct)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var orderService = scope.ServiceProvider
                .GetRequiredService<IOrderService>();

            try
            {
                var expirationHours = 24;
                var cutoffTime = DateTime.UtcNow.AddHours(-expirationHours);

                _logger.LogDebug(
                    "🔍 Looking for pending orders created before {CutoffTime}",
                    cutoffTime);

                // Get pending orders older than 24 hours
                var expiredOrders = await orderService
                    .GetPendingOrdersBeforeAsync(cutoffTime);

                _logger.LogInformation(
                    "📊 Found {Count} pending orders older than {Hours}h",
                    expiredOrders.Count,
                    expirationHours);

                int cancelledCount = 0;

                foreach (var order in expiredOrders)
                {
                    try
                    {
                        _logger.LogInformation(
                            "🗑️ Cancelling expired order {OrderId} created at {CreatedAt}",
                            order.OrderId,
                            order.CreatedAt);

                        await orderService.CancelOrderAsync(
                            order.OrderId,
                            "Auto-cancelled: Payment not received within 24 hours");

                        cancelledCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "❌ Failed to cancel order {OrderId}",
                            order.OrderId);
                    }
                }

                if (cancelledCount > 0)
                {
                    _logger.LogInformation(
                        "✅ Auto-cancelled {Count} expired orders",
                        cancelledCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in CheckExpiredOrdersAsync");
            }
        }
    }
}
```

---

## Integration Examples

### Example 1: Workers + SignalR - Real-time Updates

```csharp
// In Worker
public class NotificationWorkerService : BackgroundService
{
    private readonly IHubContext<ChatHub> _hubContext;

    private async Task ProcessDueNotificationsAsync(CancellationToken ct)
    {
        var notificationService = /* get service */;
        var count = await notificationService.DispatchDueAsync();

        if (count > 0)
        {
            // Push update to all connected clients in real-time!
            await _hubContext.Clients.All.SendAsync(
                "notificationCountUpdated",
                new { count = count, dispatchedAt = DateTime.UtcNow });
        }
    }
}

// In Frontend
connection.on("notificationCountUpdated", (data) => {
    console.log(`✅ ${data.count} notifications just sent!`);
    updateNotificationBadge(data.count);
    playNotificationSound();
});
```

### Example 2: Order Processing Pipeline

```csharp
// 1. Customer places order (Controller)
[HttpPost]
public async Task<IActionResult> PlaceOrder(CreateOrderDto dto)
{
    var orderId = await _orderService.CreateAsync(dto);
    
    // Broadcast to admin via SignalR
    await _hubContext.Clients.Group("admin")
        .SendAsync("newOrder", new { orderId, customerName = dto.CustomerName });
    
    return Ok(new { orderId });
}

// 2. Worker checks order payment (Every 1 minute)
private async Task ProcessDueNotificationsAsync(CancellationToken ct)
{
    var unpaidOrders = await _orderService.GetUnpaidOrdersAsync();
    
    foreach (var order in unpaidOrders)
    {
        if (order.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
        {
            // Send reminder via SignalR
            await _hubContext.Clients.Group($"user:{order.AccountId}")
                .SendAsync("paymentReminder", new { orderId = order.OrderId });
        }
    }
}

// 3. Customer receives notification (Frontend)
connection.on("paymentReminder", (data) => {
    showToast("⏰ Please complete your payment for order #" + data.orderId, "warning");
});

// 4. Customer completes payment (Controller)
[HttpPost]
public async Task<IActionResult> CompletePayment(int orderId)
{
    await _paymentService.ProcessAsync(orderId);
    
    // Broadcast to admin
    await _hubContext.Clients.Group("admin")
        .SendAsync("orderPaid", new { orderId });
    
    // Broadcast to customer
    await _hubContext.Clients.Group($"user:{userId}")
        .SendAsync("paymentSuccess", new { orderId });
    
    return Ok();
}
```

---

## Real-world Scenarios

### Scenario 1: Live Inventory System

```csharp
// Hub - Broadcast inventory updates
public class InventoryHub : Hub
{
    private readonly IProductService _productService;
    private readonly IHubContext<InventoryHub> _hubContext;

    public async Task RequestInventoryUpdate(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        
        // Broadcast current stock to all admins
        await Clients.Group("admin").SendAsync("inventoryUpdate", new
        {
            productId = productId,
            productName = product.Name,
            stock = product.Stock,
            lowStockWarning = product.Stock < 10,
            lastUpdated = DateTime.UtcNow
        });
    }
}

// Worker - Monitor low stock
public class InventoryWorkerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Get all products with low stock
            var lowStockProducts = await _productService
                .GetLowStockProductsAsync(threshold: 10);
            
            foreach (var product in lowStockProducts)
            {
                // Broadcast low stock alert
                await _hubContext.Clients.Group("warehouse")
                    .SendAsync("lowStockAlert", new
                    {
                        productId = product.ProductId,
                        productName = product.Name,
                        currentStock = product.Stock,
                        requiredStock = 50,
                        action = "REORDER_NEEDED"
                    });
            }
        }
    }
}
```

### Scenario 2: Live Chat with Notifications

```csharp
// ChatHub - Real-time messaging
public class ChatHub : Hub
{
    private readonly INotificationService _notificationService;

    public async Task SendChatMessage(SendMessageDto dto)
    {
        var message = await _chatService.SaveMessageAsync(dto);
        
        // Send to recipient
        await Clients.Group($"user:{dto.RecipientId}")
            .SendAsync("newMessage", message);
        
        // Create notification
        await _notificationService.CreateAsync(new NotificationDto
        {
            UserId = dto.RecipientId,
            Title = "New Message",
            Message = $"{dto.SenderName}: {dto.Content}",
            Type = "Chat",
            ActionUrl = $"/Chat/Conversation/{message.ConversationId}"
        });
    }
}

// Worker - Dispatch notifications
public class NotificationWorkerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var count = await _notificationService.DispatchDueAsync();
            
            if (count > 0)
            {
                // Broadcast to connected users
                await _hubContext.Clients.All
                    .SendAsync("newNotifications", count);
            }
        }
    }
}

// Frontend
connection.on("newMessage", (message) => {
    // Sound + toast
    playSound();
    showToast(message.senderName + ": " + message.content);
});

connection.on("newNotifications", (count) => {
    // Update badge
    document.getElementById("notif-badge").textContent = count;
});
```

---

**Created**: 2025-11-05  
**Project**: ASP_LorKingDom_PRN222
