using CodeGenerator.Patterns.Singleton;

namespace Singleton.UnitTests.UseCases;

// BASIC SINGLETON - Simple, thread-safe, lazy initialization
[Singleton]
public partial class ConfigurationManager
{
    private Dictionary<string, string> _settings;

    private void Initialize()
    {
        // This method is called once during singleton creation
        _settings = new Dictionary<string, string>
        {
            ["Environment"] = "Production",
            ["Version"] = "1.0.0",
            ["DatabaseTimeout"] = "30"
        };
        
        Console.WriteLine("ConfigurationManager initialized");
    }

    public string GetSetting(string key) => _settings.TryGetValue(key, out var value) ? value : "";
    public void SetSetting(string key, string value) => _settings[key] = value;
}

//CONVERTED TO SINGLETON
partial class ConfigurationManager
{
    private static volatile ConfigurationManager? _instance;
    private static int _isInitialized = 0;

    public static ConfigurationManager Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path
            return GetOrCreateInstance();
        }
    }

    private static ConfigurationManager GetOrCreateInstance()
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

    private static ConfigurationManager CreateSingletonInstance()
    {
        var instance = new ConfigurationManager();
        instance.Initialize();
        return instance;
    }
}