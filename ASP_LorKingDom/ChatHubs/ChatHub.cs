using BLL.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebUI.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chat;

    public ChatHub(IChatService chat) => _chat = chat;

    private (string userId, bool isStaff, string name) GetIdentity()
    {
        var q = Context.GetHttpContext()!.Request.Query;
        var uid = q["userId"].ToString();
        var staff = bool.TryParse(q["isStaff"], out var b) && b;
        var name = q["name"].ToString() ?? uid;
        return (uid, staff, name);
    }

    public override async Task OnConnectedAsync()
    {
        var (uid, staff, name) = GetIdentity();
        await _chat.UserConnectedAsync(uid, staff, name, Context.ConnectionId);
        await Clients.All.SendAsync("presenceChanged", uid, _chat.IsOnline(uid));
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var (uid, staff, _) = GetIdentity();
        await _chat.UserDisconnectedAsync(uid, staff, Context.ConnectionId);
        await Clients.All.SendAsync("presenceChanged", uid, _chat.IsOnline(uid));
    }

    /// <summary>
    /// Customer bắt đầu chat - tạo hoặc lấy conversation
    /// </summary>
    public async Task<string> StartChatAsCustomer(string customerId)
    {
        var convId = await _chat.StartChatAsCustomerAsync(customerId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{convId}");
        return convId;
    }

    /// <summary>
    /// Staff mở một conversation - join group và mark as read
    /// </summary>
    public async Task OpenByStaff(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

        var card = await _chat.OpenByStaffAsync(conversationId);
        if (card != null)
        {
            // Broadcast conversation card đã được đọc
            await Clients.All.SendAsync("convUpdated", card);
        }
    }

    /// <summary>
    /// Gửi tin nhắn trong conversation
    /// </summary>
    public async Task SendMessage(string conversationId, string senderId, string text)
    {
        var (msg, card) = await _chat.SendAsync(conversationId, senderId, text);

        // Gửi message cho tất cả người trong conversation
        await Clients.Group($"conv:{conversationId}")
            .SendAsync("receive", msg);

        // Cập nhật conversation card cho tất cả (để update unread count, last message, etc.)
        await Clients.All.SendAsync("convUpdated", card);
    }

    /// <summary>
    /// Thông báo đang typing
    /// </summary>
    public async Task Typing(string conversationId, string userId, bool isTyping)
    {
        await Clients.OthersInGroup($"conv:{conversationId}")
            .SendAsync("typing", userId, isTyping);
    }
}