using System.Collections.Concurrent;
using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.ConsoleApp;

[Singleton(Strategy = SingletonStrategy.LockFree)]
public partial class CacheManager
{
    private readonly ConcurrentDictionary<string, (object Value, DateTime Expiry)> _cache = new();

    private CacheManager()
    {
    }
    
    private void Initialize()
    {
        // Start background cleanup task
        _ = Task.Run(CleanupExpiredEntries);
        Console.WriteLine("CacheManager initialized with cleanup task");
    }

    public void Set<T>(string key, T value, TimeSpan expiry)
    {
        var expiryTime = DateTime.UtcNow.Add(expiry);
        _cache.AddOrUpdate(key, (value!, expiryTime), (k, v) => (value!, expiryTime));
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow <= entry.Expiry)
            {
                return (T)entry.Value;
            }
            // Remove expired entry
            _cache.TryRemove(key, out _);
        }
        return default(T);
    }

    private async Task CleanupExpiredEntries()
    {
        while (true)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => now > kvp.Value.Expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            await Task.Delay(TimeSpan.FromMinutes(5)); // Cleanup every 5 minutes
        }
    }
}