using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.ConsoleApp;

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