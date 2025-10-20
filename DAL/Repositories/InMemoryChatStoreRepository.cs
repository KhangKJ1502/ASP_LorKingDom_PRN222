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
                _staffPool.Add(staffId);
        }
    }

    public void UnregisterStaff(string staffId)
    {
        lock (_lock) _staffPool.Remove(staffId);
    }

    public string? PickStaff()
    {
        lock (_lock)
        {
            if (_staffPool.Count == 0) return null;
            _index = (_index + 1) % _staffPool.Count;
            return _staffPool[_index];
        }
    }
}
