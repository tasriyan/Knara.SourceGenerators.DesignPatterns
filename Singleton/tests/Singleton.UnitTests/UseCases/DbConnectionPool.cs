using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using CodeGenerator.Patterns.Singleton;

namespace Singleton.UnitTests.UseCases;

// FACTORY-BASED SINGLETON - For complex initialization scenarios
[Singleton(Strategy = SingletonStrategy.LockFree, UseFactory = true)]
public partial class DbConnectionPool
{
    private readonly string _connectionString;
    private readonly ConcurrentQueue<IDbConnection> _availableConnections;
    private readonly int _maxPoolSize;

    // Factory method for complex initialization
    public static DbConnectionPool CreateInstance()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                               ?? "Server=localhost;Database=MyApp;";
        var maxPoolSize = int.Parse(Environment.GetEnvironmentVariable("POOL_SIZE") ?? "10");
        
        return new DbConnectionPool(connectionString, maxPoolSize);
    }

    private DbConnectionPool(string connectionString, int maxPoolSize)
    {
        _connectionString = connectionString;
        _maxPoolSize = maxPoolSize;
        _availableConnections = new ConcurrentQueue<IDbConnection>();
        
        // Pre-populate the pool
        for (int i = 0; i < maxPoolSize; i++)
        {
            _availableConnections.Enqueue(CreateConnection());
        }
        
        Console.WriteLine($"DatabaseConnectionPool initialized with {maxPoolSize} connections");
    }

    public IDbConnection GetConnection()
    {
        if (_availableConnections.TryDequeue(out var connection))
        {
            return connection;
        }
        
        // Pool exhausted, create new connection
        return CreateConnection();
    }

    public void ReturnConnection(IDbConnection connection)
    {
        if (_availableConnections.Count < _maxPoolSize)
        {
            _availableConnections.Enqueue(connection);
        }
        else
        {
            connection.Dispose();
        }
    }

    private IDbConnection CreateConnection()
    {
        // Simulate creating a database connection
        return new SqlConnection(_connectionString);
    }
}

//CONVERTED TO SINGLETON
partial class DbConnectionPool
{
    private static volatile DbConnectionPool? _instance;
    private static int _isInitialized = 0;

    public static DbConnectionPool Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path
            return GetOrCreateInstance();
        }
    }

    private static DbConnectionPool GetOrCreateInstance()
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

    private static DbConnectionPool CreateSingletonInstance()
    {
        var instance = CreateInstance();
        return instance;
    }
}