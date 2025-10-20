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
            if (set.Count == 0)
                _store.Connections.TryRemove(userId, out _);
        }
        if (isStaff) _store.UnregisterStaff(userId);
        return Task.CompletedTask;
    }

    public Task<string> StartChatAsCustomerAsync(string customerId)
    {
        if (!_store.CustomerIndex.TryGetValue(customerId, out var convId)
            || !_store.Conversations.TryGetValue(convId, out var conv))
        {
            var staff = _store.PickStaff() ?? "staff1";
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
}
