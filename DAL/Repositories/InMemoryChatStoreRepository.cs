using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DAL.Interfaces;
using DAL.Models;
using System.Collections.Concurrent;

namespace DAL.Repositories;

public class InMemoryChatStoreRepository : IChatStoreRepository
{
    public ConcurrentDictionary<string, HashSet<string>> Connections { get; } = new();
    public ConcurrentDictionary<string, ChatUser> Users { get; } = new();
    public ConcurrentDictionary<string, Conversation> Conversations { get; } = new();
    public ConcurrentDictionary<string, string> CustomerIndex { get; } = new();

    public bool IsOnline(string userId) =>
        Connections.TryGetValue(userId, out var set) && set.Count > 0;

    private readonly object _lock = new();
    private readonly List<string> _staffPool = new();
    private int _index = -1;

    public void RegisterStaff(string staffId)
    {
        lock (_lock)
        {
            if (!_staffPool.Contains(staffId))
            {
                _staffPool.Add(staffId);
                Console.WriteLine($"[REGISTER STAFF] Added: {staffId}, Pool: [{string.Join(", ", _staffPool)}]");
            }
        }
    }

    public void UnregisterStaff(string staffId)
    {
        lock (_lock)
        {
            _staffPool.Remove(staffId);
            Console.WriteLine($"[UNREGISTER STAFF] Removed: {staffId}, Pool: [{string.Join(", ", _staffPool)}]");
        }
    }

    public string? PickStaff()
    {
        lock (_lock)
        {
            if (_staffPool.Count == 0)
            {
                Console.WriteLine($"[PICK STAFF] No staff in pool!");
                return null;
            }
            _index = (_index + 1) % _staffPool.Count;
            var selected = _staffPool[_index];
            Console.WriteLine($"[PICK STAFF] Selected: {selected} (index={_index}, pool=[{string.Join(", ", _staffPool)}])");
            return selected;
        }
    }
}
