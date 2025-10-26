using BLL.DTOs;

namespace BLL.Interfaces;
public interface IChatService
{
    Task UserConnectedAsync(string userId, bool isStaff, string displayName, string connectionId);
    Task UserDisconnectedAsync(string userId, bool isStaff, string connectionId);
    Task<string> StartChatAsCustomerAsync(string customerId);
    Task<ConversationDto?> OpenByStaffAsync(string conversationId);
    Task<(MessageDto, ConversationDto)> SendAsync(string conversationId, string senderId, string text);
    Task<string> StartChatWithStaffAsync(string customerId, string staffId);
    bool IsOnline(string userId);

    // THÊM CÁC METHOD NÀY
    Task<List<ConversationDto>> GetStaffConversationsAsync(string staffId);
    Task<List<MessageDto>> GetConversationMessagesAsync(string conversationId);
}