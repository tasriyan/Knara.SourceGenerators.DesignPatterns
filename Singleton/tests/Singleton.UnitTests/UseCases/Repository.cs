using System.Collections.Concurrent;
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace Singleton.UnitTests.UseCases;

// GENERIC SINGLETON - Works with type parameters
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
public partial class Repository<T> where T : IEntity
{
    private readonly ConcurrentBag<T> _items = [];

    private Repository() {
    }

    private void Initialize()
    {
        Console.WriteLine($"Repository<{typeof(T).Name}> initialized");
    }

    public void Add(T item)
    {
        lock (_lock) //_lock is added by the code generator
        {
            // do something here if needed
        }
        _items.Add(item);
    }

    public IReadOnlyList<T> GetAll()
    {
        return _items.ToList().AsReadOnly();
    }

    public T? FindById(Func<T, bool> predicate)
    {
        return _items.FirstOrDefault(predicate);
    }
}

// GENERATED CONVERSION TO SINGLETON
partial class Repository<T>
    where T : IEntity
{
    private static volatile Repository<T>? _instance;
    private static readonly object _lock = new object();

    public static Repository<T> Instance
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

    private static Repository<T> CreateSingletonInstance()
    {
        var instance = new Repository<T>();
        instance.Initialize();
        return instance;
    }
}