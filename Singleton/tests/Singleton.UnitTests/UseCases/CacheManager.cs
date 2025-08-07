using System.Collections.Concurrent;
using CodeGenerator.Patterns.Singleton;

namespace Singleton.UnitTests.UseCases;

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

//GENERATED CONVERSION TO SINGLETON
partial class CacheManager
{
    private static volatile CacheManager? _instance;
    private static int _isInitialized = 0;

    public static CacheManager Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path
            return GetOrCreateInstance();
        }
    }

    private static CacheManager GetOrCreateInstance()
    {
        if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
        {
            // We won the race - create the instance
            var newInstance = CreateSingletonInstance();
            Interlocked.Exchange(ref _instance, newInstance); // Atomic assignment with memory barrier
        }
        else
        {
            // Another thread is creating the instance - spin wait
            SpinWait.SpinUntil(() => _instance != null);
        }
        return _instance!;
    }

    private static CacheManager CreateSingletonInstance()
    {
        var instance = new CacheManager();
        instance.Initialize();
        return instance;
    }
}

