# 📚 SignalR & Worker - Complete Documentation

## 📍 File Organization

```
Documentation/
├── SignalR_Worker_Guide.md           ✅ Main guide (concepts + architecture)
├── SignalR_Worker_Examples.md        ✅ Practical code examples
├── SignalR_Worker_Troubleshooting.md ✅ Debugging & FAQs
└── README.md                         ✅ This file
```

---

## 🎯 Quick Start

### What You'll Learn

| Topic | File | Time |
|-------|------|------|
| **Concepts** | SignalR_Worker_Guide.md | 30 min |
| **SignalR Setup** | SignalR_Worker_Guide.md § SignalR | 20 min |
| **Background Workers** | SignalR_Worker_Guide.md § Workers | 20 min |
| **Code Examples** | SignalR_Worker_Examples.md | 40 min |
| **Troubleshooting** | SignalR_Worker_Troubleshooting.md | 30 min |
| **Total** | All files | ~2 hours |

---

## 📖 How to Use This Documentation

### **Beginner Path** 👶

1. Read: **SignalR_Worker_Guide.md**
   - Khái Niệm Cơ Bản
   - Cách SignalR Hoạt Động
   - Architecture

2. Code Along: **SignalR_Worker_Examples.md**
   - Example 1: Basic Chat
   - Example 2: Broadcast Notification

3. Debug: **SignalR_Worker_Troubleshooting.md**
   - Problem 1: CORS Error
   - Problem 2: Client Never Receives

### **Intermediate Path** 👨‍💻

1. Review: **SignalR_Worker_Guide.md**
   - Scoped Services in Workers
   - Exception Handling Pattern

2. Implement: **SignalR_Worker_Examples.md**
   - Example 1: Custom Worker
   - Integration Examples

3. Optimize: **SignalR_Worker_Troubleshooting.md**
   - Performance Optimization
   - Monitoring

### **Advanced Path** 🚀

1. Deep Dive: All sections in **SignalR_Worker_Guide.md**
   - Performance Tips
   - Best Practices
   - Design Patterns

2. Production: **SignalR_Worker_Examples.md**
   - Real-world Scenarios (Inventory, Chat)

3. Production Ready: **SignalR_Worker_Troubleshooting.md**
   - Health Checks
   - Monitoring Strategy

---

## 🔥 Key Takeaways

### **SignalR** 📡
- ✅ Real-time bidirectional communication
- ✅ Uses WebSocket (primary), SSE, or Long Polling
- ✅ Automatic reconnection
- ✅ Groups for targeted messaging
- ✅ Perfect for: Chat, Notifications, Live Updates

**In Your Project:**
```
✅ ChatHub: Real-time messaging
✅ Broadcast: Admin notifications
✅ Presence: User online/offline status
```

### **Background Workers** 🔄
- ✅ Runs continuously in background (IHostedService)
- ✅ Perfect for scheduled tasks
- ✅ Must use SemaphoreSlim to prevent overlap
- ✅ Must create AsyncScope for Scoped services
- ✅ Graceful shutdown support

**In Your Project:**
```
✅ NotificationWorkerService: Dispatch notifications (every 1 min)
✅ PromotionWorkerService: Check expired promotions (every 5 min)
```

---

## 🏗️ Architecture Overview

### **Layer Diagram**

```
┌─────────────────────────────────────┐
│  Browser (WebSocket Client)          │
│  - connection.on()                   │
│  - connection.invoke()               │
└────────────┬────────────────────────┘
             │ WebSocket /chatHub
             ▼
┌─────────────────────────────────────┐
│  ChatHub (SignalR Server)            │
│  - OnConnectedAsync()                │
│  - SendMessage()                     │
│  - Groups.AddToGroupAsync()          │
│  - Clients.Group().SendAsync()       │
└────────────┬────────────────────────┘
             │ Uses
             ▼
┌─────────────────────────────────────┐
│  IChatService (BLL)                  │
│  - SaveMessageAsync()                │
│  - GetConversations()                │
└────────────┬────────────────────────┘
             │ Uses
             ▼
┌─────────────────────────────────────┐
│  Database                            │
│  - Chats, Messages, UserConnections  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  App Startup (Program.cs)            │
│  - AddSignalR()                      │
│  - MapHub<ChatHub>()                 │
│  - AddHostedService<Workers>()       │
└────────────┬────────────────────────┘
             │ Runs in background
             ▼
┌─────────────────────────────────────┐
│  NotificationWorkerService           │
│  - ExecuteAsync() loops every 1 min  │
│  - DispatchDueNotifications()        │
│  - SemaphoreSlim prevents overlap    │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  PromotionWorkerService              │
│  - ExecuteAsync() loops every 5 min  │
│  - CheckExpiredPromotions()          │
│  - Auto-disable expired promotions   │
└─────────────────────────────────────┘
```

---

## 🚀 Implementation Checklist

### **Phase 1: SignalR Setup** ✅

- [ ] Add SignalR in `Program.cs`
  ```csharp
  builder.Services.AddSignalR();
  app.MapHub<ChatHub>("/chatHub");
  ```

- [ ] Create ChatHub
  ```csharp
  public class ChatHub : Hub { ... }
  ```

- [ ] Configure CORS (if needed)
  ```csharp
  .AllowCredentials() // ✅ CRITICAL for SignalR
  ```

- [ ] Add client-side script
  ```html
  <script src="~/lib/signalr/signalr.js"></script>
  ```

- [ ] Initialize connection
  ```javascript
  const connection = new signalR.HubConnectionBuilder()
      .withUrl("/chatHub")
      .build();
  connection.start();
  ```

### **Phase 2: Worker Setup** ✅

- [ ] Create NotificationWorkerService (or copy from example)
- [ ] Create PromotionWorkerService (or copy from example)
- [ ] Register in `Program.cs`
  ```csharp
  builder.Services.AddHostedService<NotificationWorkerService>();
  builder.Services.AddHostedService<PromotionWorkerService>();
  ```
- [ ] Test with logging
- [ ] Set proper intervals

### **Phase 3: Integration** ✅

- [ ] Hub methods call BLL services
- [ ] Workers create AsyncScope for DI
- [ ] Error handling with try-catch-finally
- [ ] Logging at each step
- [ ] SemaphoreSlim to prevent overlap

### **Phase 4: Testing** ✅

- [ ] Test SignalR connection (DevTools)
- [ ] Test message sending/receiving
- [ ] Test worker execution
- [ ] Test error scenarios
- [ ] Test under load

### **Phase 5: Deployment** ✅

- [ ] Configure logging for production
- [ ] Set appropriate intervals
- [ ] Monitor worker health
- [ ] Set up alerts
- [ ] Document for ops team

---

## 📊 Performance Metrics

### **Expected Performance**

| Metric | Target | Notes |
|--------|--------|-------|
| Message latency | <500ms | SignalR → DB → broadcast |
| Worker interval | 1-5 min | Depends on use case |
| Concurrent connections | 1000+ | Per server |
| Message throughput | 100+ msg/sec | Per hub |
| Worker overlap | 0% | Semaphore ensures this |

### **Monitoring**

```csharp
// Add to logging
_logger.LogInformation("Processed {Count} items in {Ms}ms",
    count, stopwatch.ElapsedMilliseconds);

// Monitor:
// 1. Message latency
// 2. Worker execution time
// 3. Database query time
// 4. Connection count
// 5. Error rate
```

---

## 🔗 Related Documentation

- [Architecture Overview](./Architecture/)
- [State Charts](./StateCharts/) - Notification states, Order states
- [Database Schema](../Database/)
- [API Documentation](../API/)

---

## 💡 Tips & Tricks

### **SignalR Tips**

| Tip | Benefit |
|-----|---------|
| Use Groups for targeted messages | Reduce bandwidth |
| Compress large payloads | Faster transfer |
| Implement auto-reconnect | Better UX |
| Log connection events | Easier debugging |
| Use MessagePack protocol | Binary, 20% smaller |

### **Worker Tips**

| Tip | Benefit |
|-----|---------|
| Use SemaphoreSlim | Prevent data corruption |
| Create AsyncScope | Avoid DI errors |
| Add jitter to startup | Prevent thundering herd |
| Batch database operations | Faster processing |
| Monitor execution time | Detect slowdowns |

---

## 🐛 Common Mistakes

### **❌ SignalR Mistakes**

1. **Not calling `.AllowCredentials()`**
   - ✅ Fix: Add to CORS policy

2. **Listener registered after `.start()`**
   - ✅ Fix: Register `.on()` BEFORE `.start()`

3. **Wrong group name format**
   - ✅ Fix: Use consistent naming like `user:{userId}`

4. **Ignoring connection state**
   - ✅ Fix: Check `connection.state` before invoking

### **❌ Worker Mistakes**

1. **Injecting Scoped service into Singleton**
   - ✅ Fix: Inject `IServiceProvider`, create scope

2. **Not using SemaphoreSlim**
   - ✅ Fix: Add `_gate` to prevent overlap

3. **Not catching OperationCanceledException**
   - ✅ Fix: Catch separately for graceful shutdown

4. **Setting interval too aggressive (<1 min)**
   - ✅ Fix: Increase interval, batch processing

---

## 📞 Getting Help

### **Debugging Steps**

1. **Check Logs**
   ```bash
   # See all SignalR/Worker logs
   tail -f logs/app.log | grep -i "signalr\|worker\|notification"
   ```

2. **Test Hub Connection**
   ```javascript
   // In browser console
   console.log(connection.state); // Should be 1 (Connected)
   console.log(connection.connectionId); // Should have value
   ```

3. **Check Network**
   - Open DevTools → Network tab
   - Look for WebSocket connection to /chatHub
   - Check status: 101 (Switching Protocols)

4. **Test Worker**
   - Check logs for worker start message
   - Verify interval timing
   - Monitor database for changes

### **Common Resources**

- [Microsoft SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [IHostedService Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice)
- [ASP.NET Core Logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/)

---

## 📝 Document Version

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-05 | Initial documentation |
| | | - SignalR guide & examples |
| | | - Worker guide & examples |
| | | - Troubleshooting guide |
| | | - FAQs & best practices |

---

## 👥 Contributors

**Original Project**: ASP_LorKingDom_PRN222  
**Owner**: KhangKJ1502  
**Created**: 2025-11-05

---

## 📞 Support

For questions or issues:
1. Check **SignalR_Worker_Troubleshooting.md**
2. Review **SignalR_Worker_Examples.md** for similar patterns
3. Check **SignalR_Worker_Guide.md** for concepts
4. Add logging and review console output

---

## 🎓 Learning Path

```
Day 1: Read Guide + Architecture (2 hours)
Day 2: Implement Basic Chat (3 hours)
Day 3: Implement Workers (2 hours)
Day 4: Integration + Testing (3 hours)
Day 5: Production Optimization (2 hours)
```

---

**Happy Coding! 🚀**
