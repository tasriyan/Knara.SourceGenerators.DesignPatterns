using CodeGenerator.Patterns.Singleton;

namespace Singleton.UnitTests.UseCases;

// EAGER SINGLETON
[Singleton(Strategy = SingletonStrategy.Eager)]
public partial class Logger
{
    private string _logFilePath;
    private readonly object _lock = new object();

    private Logger()
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
            Console.WriteLine(logEntry + Environment.NewLine);
        }
    }

    public static void LogMessage(string message)
    {
        Console.WriteLine($"Logging message... {message}");;
    }
}

// GENERATED CONVERSION TO SINGLETON
partial class Logger
{
    private static readonly Logger _instance = CreateSingletonInstance();

    static Logger() { } // Explicit static constructor to ensure beforefieldinit behavior

    public static Logger Instance => _instance;

    private static Logger CreateSingletonInstance()
    {
        var instance = new Logger();
        return instance;
    }
}