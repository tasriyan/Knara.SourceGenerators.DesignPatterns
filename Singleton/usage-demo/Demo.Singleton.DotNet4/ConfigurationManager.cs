using System.Collections.Concurrent;
using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.DotNet4
{
    // BASIC SINGLETON - Simple, thread-safe, lazy initialization
    [Singleton(Strategy = SingletonStrategy.Lazy)]
    public partial class ConfigurationManager
    {
        private readonly ConcurrentDictionary<string, string> _settings = new ConcurrentDictionary<string, string>
        {
            ["Environment"] = "Production",
            ["Version"] = "1.0.0",
            ["DatabaseTimeout"] = "30"
        };

        private ConfigurationManager()
        {
            Console.WriteLine("ConfigurationManager initialized");
        }
    
        public void LoadSettings()
        {
            // Simulate loading settings from a file or database
            Console.WriteLine("Loading settings...");
            _settings["Environment"] = "Development";
            _settings["Version"] = "1.0.1";
            _settings["DatabaseTimeout"] = "60";
        }

        public string GetSetting(string key) => _settings.TryGetValue(key, out var value) ? value : "";
        public void SetSetting(string key, string value) => _settings[key] = value;
    
        public static void LogMessage(string message)
        {
            Console.WriteLine($"Logging message... {message}");;
        }
    }
}