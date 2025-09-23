using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace Singleton.UnitTests.UseCases;

[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
public partial class DbConnectionPool
{
    private readonly string _connectionString;
    private readonly ConcurrentQueue<IDbConnection> _availableConnections;
    private readonly int _maxPoolSize;

    private DbConnectionPool()
    {
        _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                            ?? "Server=localhost;Database=MyApp;";
        _maxPoolSize = int.Parse(Environment.GetEnvironmentVariable("POOL_SIZE") ?? "10");
        _availableConnections = new ConcurrentQueue<IDbConnection>();
        
        // Pre-populate the pool
        for (int i = 0; i < _maxPoolSize; i++)
        {
            _availableConnections.Enqueue(CreateConnection());
        }
        
        Console.WriteLine($"DatabaseConnectionPool initialized with {_maxPoolSize} connections");
    }

    public IDbConnection GetConnection()
    {
        if (_availableConnections.TryDequeue(out var connection))
        {
            Console.WriteLine($"Retrieved connection: {connection != null}");
            return connection;
        }
        
        // Pool exhausted, create new connection
        Console.WriteLine("Connection pool exhausted, creating new connection");
        return CreateConnection();
    }

    public void ReturnConnection(IDbConnection connection)
    {
        if (_availableConnections.Count < _maxPoolSize)
        {
            _availableConnections.Enqueue(connection);
            Console.WriteLine($"Returning connection: {connection != null}");
        }
        else
        {
            connection.Dispose();
            Console.WriteLine("Connection pool is full, disposing connection");
        }
    }

    private IDbConnection CreateConnection()
    {
        // Simulate creating a database connection
        return new SqlConnection(_connectionString);
    }
}

// GENERATED CONVERSION TO SINGLETON
partial class DbConnectionPool
{
    private static volatile DbConnectionPool? _instance;
    private static readonly object _lock = new object();

    public static DbConnectionPool Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = CreateSingletonInstance();
                    }
                }
            }
            return _instance;
        }
    }

    private static DbConnectionPool CreateSingletonInstance()
    {
        var instance = new DbConnectionPool();
        return instance;
    }
}