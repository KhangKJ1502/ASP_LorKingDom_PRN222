using System.Collections.Concurrent;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebUI.Hubs;

/// CHỨC NĂNG:
/// 1. Real-time messaging (send/receive)
/// 2. Online/Offline presence tracking
/// 3. Typing indicators
/// 4. Conversation management
/// 5. Staff page state tracking (on chat page hay không)
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    // Lưu trạng thái staff trong memory (lost khi restart server)
    private static readonly ConcurrentDictionary<string, int> _staffConnCount = new();     // staffId -> số connections
    private static readonly ConcurrentDictionary<string, string> _staffNames = new();      // staffId -> tên hiển thị
    private static readonly ConcurrentDictionary<string, bool> _staffPageState = new();    // staffId -> có đang ở trang chat không

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// Query string helper - lấy value từ query parameter
    private static string Q(HttpContext? http, string key)
        => http?.Request.Query[key].ToString() ?? string.Empty;

    /// Query string helper - parse boolean
    private static bool QBool(HttpContext? http, string key)
        => bool.TryParse(Q(http, key), out var b) && b;

    /// Broadcast online/offline status đến tất cả clients
    private async Task BroadcastPresence(string userId, bool isOnline)
    {
        try
        {
            await Clients.All.SendAsync("presenceChanged", userId, isOnline);
            _logger.LogInformation("Presence: {UserId} = {Status}", userId, isOnline ? "🟢 ONLINE" : "⚫ OFFLINE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting presence");
        }
    }

    /// Kiểm tra staff có đang ở trang chat không (để biết có cần gửi notification không)
    private bool IsStaffOnChatPage(string staffId)
        => _staffPageState.TryGetValue(staffId, out var on) && on;
    
    /// Được gọi khi client connect vào SignalR hub
    /// Query params: ?userId=xxx&isStaff=true/false&name=xxx
    public override async Task OnConnectedAsync()
    {
        try
        {
            var http = Context.GetHttpContext();
            var userId = Q(http, "userId");
            var isStaff = QBool(http, "isStaff");
            var name = Q(http, "name");

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Connection rejected: missing userId");
                Context.Abort();
                return;
            }


            // Lưu connection vào database (mapping userId ↔ connectionId)
            await _chatService.UserConnectedAsync(userId, isStaff, string.IsNullOrWhiteSpace(name) ? "Unknown" : name, Context.ConnectionId);
            
            // Add vào SignalR group để có thể gửi message targeted
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            if (isStaff)
            {
                // Cập nhật in-memory state
                _staffNames[userId] = string.IsNullOrWhiteSpace(name) ? userId : name!;
                _staffConnCount.AddOrUpdate(userId, 1, (_, n) => n + 1);
                _staffPageState[userId] = true;

                // Nếu là connection đầu tiên của staff này → broadcast presence
                if (_staffConnCount[userId] == 1)
                {
                    await Clients.All.SendAsync("staffPresence", new { staffId = userId, name = _staffNames[userId], online = true });
                }

                // Gửi danh sách conversations cho staff
                var conversations = await _chatService.GetStaffConversationsAsync(userId);
                await Clients.Caller.SendAsync("conversationList", conversations);
                _logger.LogInformation("Staff {UserId} connected - {Count} conversations sent", userId, conversations.Count());
            }
            else
            {
                _logger.LogInformation("Customer {UserId} connected", userId);
            }

            // Broadcast online status
            await BroadcastPresence(userId, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
            throw;
        }

        await base.OnConnectedAsync();
    }

    /// Được gọi khi client disconnect khỏi SignalR hub
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var http = Context.GetHttpContext();
            var userId = Q(http, "userId");
            var isStaff = QBool(http, "isStaff");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                // Xóa connection mapping trong database
                await _chatService.UserDisconnectedAsync(userId, isStaff, Context.ConnectionId);

                // ===== XỬ LÝ STAFF DISCONNECT =====
                if (isStaff && _staffConnCount.TryGetValue(userId, out var n))
                {
                    var newN = Math.Max(0, n - 1);
                    
                    // Nếu không còn connection nào → remove khỏi in-memory state
                    if (newN == 0)
                    {
                        _staffConnCount.TryRemove(userId, out _);
                        _staffPageState.TryRemove(userId, out _);

                        var display = _staffNames.TryGetValue(userId, out var nm) ? nm : userId;
                        await Clients.All.SendAsync("staffPresence", new { staffId = userId, name = display, online = false });
                    }
                    else
                    {
                        _staffConnCount[userId] = newN;
                    }
                }

                // Nếu user không còn connection nào (check trong database) → broadcast offline
                if (!_chatService.IsOnline(userId))
                {
                    await BroadcastPresence(userId, false);
                    _logger.LogInformation("User {UserId} fully disconnected", userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync");
        }

        await base.OnDisconnectedAsync(exception);
    }

    
    /// [CUSTOMER] Bắt đầu chat (auto-assign staff)
    /// Called from: _ChatWidget.cshtml
    /// Returns: conversationId
    public async Task<string> StartChatAsCustomer(string customerId)
    {
        try
        {
            // Bỏ dan vao staff comment block dưới để yêu cầu đăng nhập StartChatAsCustomer
            //if (string.IsNullOrWhiteSpace(customerId) || customerId.StartsWith("guest_"))
            //{
            //    _logger.LogWarning("StartChatAsCustomer rejected: guest user - customerId: {CustomerId}", customerId);
            //    throw new UnauthorizedAccessException("Vui lòng đăng nhập để sử dụng tính năng chat.");
            //}

            // Tìm hoặc tạo conversation mới với staff available
            var conversationId = await _chatService.StartChatAsCustomerAsync(customerId);
            var conversation = await _chatService.OpenByStaffAsync(conversationId);

            // Add caller vào SignalR group để nhận messages
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
            
            // Notify staff về conversation mới
            if (conversation?.StaffUserId is { } sid)
            {
                await Clients.Group($"user:{sid}").SendAsync("conversationUpdated", conversation);
            }

            _logger.LogInformation("Customer {CustomerId} started chat - ConvId: {ConvId}", customerId, conversationId);
            return conversationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error StartChatAsCustomer {CustomerId}", customerId);
            throw;
        }
    }


    /// [CUSTOMER] Bắt đầu chat với staff cụ thể
    /// Called from: Customer UI (khi chọn staff từ danh sách online)
    /// Returns: conversationId
    public async Task<string> StartChatWithStaff(string customerId, string staffId)
    {
        try
        {
            // Tìm hoặc tạo conversation với staff được chọn
            var conversationId = await _chatService.StartChatWithStaffAsync(customerId, staffId);

            // Add caller vào SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

            // Notify staff về conversation mới
            var conv = await _chatService.OpenByStaffAsync(conversationId);
            if (conv != null)
            {
                await Clients.Group($"user:{staffId}").SendAsync("conversationUpdated", conv);
            }

            _logger.LogInformation("Customer {CustomerId} started chat with staff {StaffId} - ConvId: {ConvId}", customerId, staffId, conversationId);
            return conversationId;
        }
        catch (NotImplementedException)
        {
            // Fallback: nếu chưa implement StartChatWithStaffAsync → dùng auto-assign
            _logger.LogWarning("StartChatWithStaffAsync not implemented, falling back to auto-assign");
            return await StartChatAsCustomer(customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error StartChatWithStaff customer {CustomerId} staff {StaffId}", customerId, staffId);
            throw;
        }
    }

    /// [UI] Lấy danh sách staff đang online
    /// Returns: [{ staffId, name }]
    public Task<IEnumerable<object>> GetOnlineStaff()
    {
        var list = _staffConnCount
            .Where(kv => kv.Value > 0)
            .Select(kv => new
            {
                staffId = kv.Key,
                name = _staffNames.TryGetValue(kv.Key, out var nm) ? nm : kv.Key
            })
            .Cast<object>()
            .ToList()
            .AsEnumerable();

        _logger.LogInformation("📋 GetOnlineStaff: {Count} staff(s) online", list.Count());
        return Task.FromResult(list);
    }


    /// [STAFF] Lấy danh sách conversations
    /// Called from: Staff.cshtml on page load
    /// Returns: List của ConversationDto
    public async Task<List<ConversationDto>> GetConversations()
    {
        try
        {
            var http = Context.GetHttpContext();
            var staffId = Q(http, "userId");
            if (string.IsNullOrWhiteSpace(staffId)) return new List<ConversationDto>();

            var conversations = await _chatService.GetStaffConversationsAsync(staffId);
            _logger.LogInformation("GetConversations for staff {StaffId}: {Count} conversations", staffId, conversations.Count);
            return conversations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return new List<ConversationDto>();
        }
    }


    /// [CUSTOMER/STAFF] Lấy danh sách messages của một conversation
    /// Called from: Staff.cshtml / _ChatWidget.cshtml khi mở conversation
    /// Returns: List của MessageDto

    public async Task<List<MessageDto>> GetMessages(string conversationId)
    {
        try
        {
            var messages = await _chatService.GetConversationMessagesAsync(conversationId);
            _logger.LogInformation("GetMessages for conv {ConvId}: {Count} messages", conversationId, messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for {ConversationId}", conversationId);
            return new List<MessageDto>();
        }
    }


    /// [STAFF] Mở một conversation (set unread = 0)
    /// Called from: Staff.cshtml khi click vào conversation
    public async Task OpenConversation(string conversationId)
    {
        try
        {
            var conversation = await _chatService.OpenByStaffAsync(conversationId);
            if (conversation != null)
            {
                // Add connection vào conversation group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
                
                // Gửi conversation đã update về cho caller
                await Clients.Caller.SendAsync("conversationUpdated", conversation);
                _logger.LogInformation("Staff opened conversation {ConvId}", conversationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening conversation {ConversationId}", conversationId);
        }
    }

    /// [CUSTOMER/STAFF] Gửi tin nhắn
    /// Called from: _ChatWidget.cshtml / Staff.cshtml
    /// Flow:
    ///   1. Save message vào database
    ///   2. Broadcast message đến tất cả clients trong conversation group
    ///   3. Update conversation metadata (lastMessage, unread count)

    public async Task SendMessage(string conversationId, string senderId, string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("SendMessage rejected: empty message");
                return;
            }


            // Bỏ comment block dưới để yêu cầu đăng nhập
            //if (string.IsNullOrWhiteSpace(senderId) || senderId.StartsWith("guest_"))
            //{
            //    _logger.LogWarning("SendMessage rejected: guest user attempt - sender: {Sender}", senderId);
            //    throw new UnauthorizedAccessException("Vui lòng đăng nhập để gửi tin nhắn.");
            //}

            _logger.LogInformation("SendMessage - Conv:{Conv}, From:{From}, Text:{Text}", conversationId, senderId, text.Substring(0, Math.Min(50, text.Length)));

            // BƯỚC 1: Lưu message vào database
            var (message, conversation) = await _chatService.SendAsync(conversationId, senderId, text);

            // BƯỚC 2: Broadcast message realtime đến tất cả clients trong conversation
            await Clients.Group($"conv:{conversationId}").SendAsync("receive", message);

            // BƯỚC 3: Update conversation metadata cho staff
            if (!string.IsNullOrWhiteSpace(conversation.StaffUserId))
            {
                var sid = conversation.StaffUserId!;
                var onPage = IsStaffOnChatPage(sid);
                _logger.LogInformation("Staff {Staff} - OnPage: {OnPage}", sid, onPage ? "✅ YES" : "❌ NO");

                // Update lastMessage, unreadForStaff trong sidebar
                await Clients.Group($"user:{sid}").SendAsync("conversationUpdated", conversation);
            }

            _logger.LogInformation("Message delivered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            throw;
        }
    }

 
    /// [CUSTOMER/STAFF] Broadcast typing indicator
    /// Called from: Client khi user đang gõ tin nhắn
    /// Gửi đến: Tất cả clients khác trong conversation (excluding caller)
    public async Task Typing(string conversationId, string userId, bool isTyping)
    {
        try
        {
            await Clients.OthersInGroup($"conv:{conversationId}").SendAsync("typing", userId, isTyping);
            _logger.LogInformation("Typing indicator - Conv:{Conv}, User:{User}, IsTyping:{IsTyping}", conversationId, userId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting typing");
        }
    }

    /// [STAFF] Update page state (có đang ở trang chat không)
    /// Called from: Staff.cshtml on page visibility change
    /// Mục đích: Biết staff có đang xem chat không để quyết định có gửi notification không
 
    public Task UpdatePageState(bool isOnPage)
    {
        try
        {
            var http = Context.GetHttpContext();
            var userId = Q(http, "userId");
            var isStaff = QBool(http, "isStaff");

            if (!string.IsNullOrWhiteSpace(userId) && isStaff)
            {
                _staffPageState[userId] = isOnPage;
                _logger.LogInformation("UpdatePageState - Staff:{Staff} State:{State}",
                    userId, isOnPage ? "ON CHAT PAGE" : " OFF CHAT PAGE");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Error updating page state");
        }

        return Task.CompletedTask;
    }
}

