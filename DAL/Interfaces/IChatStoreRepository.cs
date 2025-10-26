using System.Collections.Concurrent;
using DAL.Models;

namespace DAL.Interfaces;

public interface IChatStoreRepository
{
    ConcurrentDictionary<string, HashSet<string>> Connections { get; }
    ConcurrentDictionary<string, ChatUser> Users { get; }
    ConcurrentDictionary<string, Conversation> Conversations { get; }
    ConcurrentDictionary<string, string> CustomerIndex { get; }

    bool IsOnline(string userId);

    void RegisterStaff(string staffId);
    void UnregisterStaff(string staffId);
    string? PickStaff();
}
