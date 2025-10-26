using System.Collections.Concurrent;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebUI.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    // --- Presence & Page-state tracking (in-memory) ---
    private static readonly ConcurrentDictionary<string, int> _staffConnCount = new(); // staffId -> #connections
    private static readonly ConcurrentDictionary<string, string> _staffNames = new();    // staffId -> displayName
    private static readonly ConcurrentDictionary<string, bool> _staffPageState = new(); // staffId -> onChatPage

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    // Helpers
    private static string Q(HttpContext? http, string key)
        => http?.Request.Query[key].ToString() ?? string.Empty;

    private static bool QBool(HttpContext? http, string key)
        => bool.TryParse(Q(http, key), out var b) && b;

    private async Task BroadcastPresence(string userId, bool isOnline)
    {
        try
        {
            await Clients.All.SendAsync("presenceChanged", userId, isOnline);
            _logger.LogInformation("📢 Presence: {UserId} = {Status}", userId, isOnline ? "ONLINE" : "OFFLINE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting presence");
        }
    }

    private bool IsStaffOnChatPage(string staffId)
        => _staffPageState.TryGetValue(staffId, out var on) && on;

    // Connection lifecycle
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
                _logger.LogWarning("❌ Connection rejected: missing userId");
                Context.Abort();
                return;
            }

            await _chatService.UserConnectedAsync(userId, isStaff, string.IsNullOrWhiteSpace(name) ? "Unknown" : name, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            if (isStaff)
            {
                _staffNames[userId] = string.IsNullOrWhiteSpace(name) ? userId : name!;
                _staffConnCount.AddOrUpdate(userId, 1, (_, n) => n + 1);
                _staffPageState[userId] = true;

                if (_staffConnCount[userId] == 1)
                {
                    await Clients.All.SendAsync("staffPresence", new { staffId = userId, name = _staffNames[userId], online = true });
                }

                var conversations = await _chatService.GetStaffConversationsAsync(userId);
                await Clients.Caller.SendAsync("conversationList", conversations);
                _logger.LogInformation("✅ Staff {UserId} connected - Conversation list sent", userId);
            }
            else
            {
                _logger.LogInformation("✅ Customer {UserId} connected", userId);
            }

            await BroadcastPresence(userId, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in OnConnectedAsync");
            throw;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var http = Context.GetHttpContext();
            var userId = Q(http, "userId");
            var isStaff = QBool(http, "isStaff");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _chatService.UserDisconnectedAsync(userId, isStaff, Context.ConnectionId);

                if (isStaff && _staffConnCount.TryGetValue(userId, out var n))
                {
                    var newN = Math.Max(0, n - 1);
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

                if (!_chatService.IsOnline(userId))
                {
                    await BroadcastPresence(userId, false);
                    _logger.LogInformation("✅ User {UserId} fully disconnected", userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in OnDisconnectedAsync");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // RPCs

    /// Customer auto-assign staff
    public async Task<string> StartChatAsCustomer(string customerId)
    {
        try
        {
            var conversationId = await _chatService.StartChatAsCustomerAsync(customerId);
            var conversation = await _chatService.OpenByStaffAsync(conversationId);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
            if (conversation?.StaffUserId is { } sid)
            {
                await Clients.Group($"user:{sid}").SendAsync("conversationUpdated", conversation);
            }

            return conversationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error StartChatAsCustomer {CustomerId}", customerId);
            throw;
        }
    }

    /// Customer chọn staff cụ thể
    public async Task<string> StartChatWithStaff(string customerId, string staffId)
    {
        try
        {
            var conversationId = await _chatService.StartChatWithStaffAsync(customerId, staffId);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

            var conv = await _chatService.OpenByStaffAsync(conversationId);
            if (conv != null)
            {
                await Clients.Group($"user:{staffId}").SendAsync("conversationUpdated", conv);
            }

            return conversationId;
        }
        catch (NotImplementedException)
        {
            return await StartChatAsCustomer(customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error StartChatWithStaff customer {CustomerId} staff {StaffId}", customerId, staffId);
            throw;
        }
    }

    /// DS staff đang online (để UI cho khách chọn)
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

        return Task.FromResult(list);
    }

    public async Task<List<ConversationDto>> GetConversations()
    {
        try
        {
            var http = Context.GetHttpContext();
            var staffId = Q(http, "userId");
            if (string.IsNullOrWhiteSpace(staffId)) return new List<ConversationDto>();

            return await _chatService.GetStaffConversationsAsync(staffId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting conversations");
            return new List<ConversationDto>();
        }
    }

    public async Task<List<MessageDto>> GetMessages(string conversationId)
    {
        try
        {
            return await _chatService.GetConversationMessagesAsync(conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting messages for {ConversationId}", conversationId);
            return new List<MessageDto>();
        }
    }

    public async Task OpenConversation(string conversationId)
    {
        try
        {
            var conversation = await _chatService.OpenByStaffAsync(conversationId);
            if (conversation != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
                await Clients.Caller.SendAsync("conversationUpdated", conversation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error opening conversation {ConversationId}", conversationId);
        }
    }

    public async Task SendMessage(string conversationId, string senderId, string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            _logger.LogInformation("📤 SendMessage - Conv:{Conv}, From:{From}", conversationId, senderId);
            var (message, conversation) = await _chatService.SendAsync(conversationId, senderId, text);

            await Clients.Group($"conv:{conversationId}").SendAsync("receive", message);

            if (!string.IsNullOrWhiteSpace(conversation.StaffUserId))
            {
                var sid = conversation.StaffUserId!;
                var onPage = IsStaffOnChatPage(sid);
                _logger.LogInformation("📍 Staff {Staff} - OnPage: {OnPage}", sid, onPage ? "YES" : "NO");

                await Clients.Group($"user:{sid}").SendAsync("conversationUpdated", conversation);

                if (senderId != sid)
                {
                    await Clients.Group($"user:{sid}").SendAsync("receive", message);
                }
            }

            _logger.LogInformation("✅ Message delivered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending message");
            throw;
        }
    }

    public async Task Typing(string conversationId, string userId, bool isTyping)
    {
        try
        {
            await Clients.OthersInGroup($"conv:{conversationId}").SendAsync("typing", userId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error broadcasting typing");
        }
    }

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
                _logger.LogInformation("📍 UpdatePageState - Staff:{Staff} State:{State}",
                    userId, isOnPage ? "ON CHAT PAGE" : "OFF CHAT PAGE");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating page state");
        }

        return Task.CompletedTask;
    }
}
