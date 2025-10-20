using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models;

public class Conversation
{
    public string ConversationId { get; init; } = Guid.NewGuid().ToString("N");
    public string CustomerUserId { get; init; } = default!;
    public string StaffUserId { get; set; } = default!;
    public DateTime LastActivityAt { get; private set; } = DateTime.UtcNow;

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyList<ChatMessage> Messages => _messages;

    public int UnreadForStaff { get; private set; }
    public int UnreadForCustomer { get; private set; }

    public ChatMessage? Last => _messages.LastOrDefault();

    public void Add(ChatMessage msg, bool fromStaff)
    {
        _messages.Add(msg);
        LastActivityAt = msg.SentAt;
        if (fromStaff) UnreadForCustomer++; else UnreadForStaff++;
    }

    public void ReadByStaff() => UnreadForStaff = 0;
    public void ReadByCustomer() => UnreadForCustomer = 0;
}
