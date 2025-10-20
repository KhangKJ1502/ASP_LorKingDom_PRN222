using BLL.DTOs;


namespace BLL.Interfaces;

public interface IChatService
{
    Task UserConnectedAsync(string userId, bool isStaff, string displayName, string connectionId);
    Task UserDisconnectedAsync(string userId, bool isStaff, string connectionId);

    Task<string> StartChatAsCustomerAsync(string customerId);
    Task<ConversationDto?> OpenByStaffAsync(string conversationId);
    Task<(MessageDto Message, ConversationDto Card)> SendAsync(string conversationId, string senderId, string text);

    bool IsOnline(string userId);
}
