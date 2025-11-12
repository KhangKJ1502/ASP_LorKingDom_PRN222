using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services;

public class ChatService : IChatService
{
    private readonly IChatStoreRepository _store;
    public ChatService(IChatStoreRepository store) => _store = store;

    public Task UserConnectedAsync(string userId, bool isStaff, string displayName, string connectionId)
    {
        _store.Users.AddOrUpdate(userId,
            _ => new ChatUser
            {
                UserId = userId,
                DisplayName = displayName,
                IsStaff = isStaff
            },
            (_, old) =>
            {
                old.DisplayName = displayName;
                old.IsStaff = isStaff;
                return old;
            });

        var set = _store.Connections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (set) set.Add(connectionId);

        if (isStaff) _store.RegisterStaff(userId);
        return Task.CompletedTask;
    }

    public Task UserDisconnectedAsync(string userId, bool isStaff, string connectionId)
    {
        if (_store.Connections.TryGetValue(userId, out var set))
        {
            lock (set) set.Remove(connectionId);
            if (set.Count == 0) _store.Connections.TryRemove(userId, out _);
        }
        if (isStaff) _store.UnregisterStaff(userId);
        return Task.CompletedTask;
    }

    public Task<string> StartChatAsCustomerAsync(string customerId)
    {
        if (!_store.CustomerIndex.TryGetValue(customerId, out var convId)
            || !_store.Conversations.TryGetValue(convId, out var conv))
        {
            var staff = _store.PickStaff() ?? "1";
            conv = new Conversation { CustomerUserId = customerId, StaffUserId = staff };
            _store.Conversations[conv.ConversationId] = conv;
            _store.CustomerIndex[customerId] = conv.ConversationId;
        }
        return Task.FromResult(conv.ConversationId);
    }

    public Task<ConversationDto?> OpenByStaffAsync(string conversationId)
    {
        if (!_store.Conversations.TryGetValue(conversationId, out var conv))
            return Task.FromResult<ConversationDto?>(null);

        conv.ReadByStaff();
        return Task.FromResult<ConversationDto?>(ToDto(conv));
    }

    public Task<(MessageDto, ConversationDto)> SendAsync(string conversationId, string senderId, string text)
    {
        if (!_store.Conversations.TryGetValue(conversationId, out var conv))
            throw new InvalidOperationException("Invalid conversation");

        var msg = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Text = text.Trim()
        };

        var fromStaff = senderId == conv.StaffUserId;
        conv.Add(msg, fromStaff);

        var mDto = new MessageDto(msg.Id, msg.ConversationId, msg.SenderId, msg.Text, msg.SentAt);
        return Task.FromResult((mDto, ToDto(conv)));
    }

    private ConversationDto ToDto(Conversation conv) =>
        new(conv.ConversationId, conv.CustomerUserId, conv.StaffUserId,
            conv.Last?.Text, conv.Last?.SentAt, conv.UnreadForStaff, _store.IsOnline(conv.CustomerUserId));

    public bool IsOnline(string userId) => _store.IsOnline(userId);

    // Lấy danh sách conversation của staff
    public Task<List<ConversationDto>> GetStaffConversationsAsync(string staffId)
    {
        var conversations = _store.Conversations.Values
            .Where(c => c.StaffUserId == staffId)
            .OrderByDescending(c => c.LastActivityAt)
            .Select(c => ToDto(c))
            .ToList();

        return Task.FromResult(conversations);
    }

    // Lấy lịch sử tin nhắn
    public Task<List<MessageDto>> GetConversationMessagesAsync(string conversationId)
    {
        if (!_store.Conversations.TryGetValue(conversationId, out var conv))
            return Task.FromResult(new List<MessageDto>());

        var messages = conv.Messages
            .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderId, m.Text, m.SentAt))
            .ToList();

        return Task.FromResult(messages);
    }

    // ✅ IMPLEMENT HOÀN CHỈNH
    public async Task<string> StartChatWithStaffAsync(string customerId, string staffId)
    {
        // Nếu khách đã có hội thoại → đổi/giữ staff theo yêu cầu
        if (_store.CustomerIndex.TryGetValue(customerId, out var existedId) &&
            _store.Conversations.TryGetValue(existedId, out var existedConv))
        {
            existedConv.StaffUserId = staffId;
            existedConv.LastActivityAt = DateTime.UtcNow;
            _store.Conversations[existedConv.ConversationId] = existedConv;
            return existedConv.ConversationId;
        }
        // Tạo mới
        var conv = new Conversation
        {
            CustomerUserId = customerId,
            StaffUserId = staffId
        };
        _store.Conversations[conv.ConversationId] = conv;
        _store.CustomerIndex[customerId] = conv.ConversationId;

        return await Task.FromResult(conv.ConversationId);
    }
}
