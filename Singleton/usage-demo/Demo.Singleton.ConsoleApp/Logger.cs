using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.ConsoleApp;

// EAGER SINGLETON - Pre-initialized at startup for ultra-fast access
[Singleton(Strategy = SingletonStrategy.Eager, ThreadSafe = true)]
public partial class Logger
{
    private string _logFilePath;
    private readonly object _lock = new object();

    private void Initialize()
    {
        _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
        Console.WriteLine($"Logger initialized with path: {_logFilePath}");
    }

    public void Log(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        lock (_lock)
        {
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }
    }
}