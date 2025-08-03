using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.ConsoleApp;

// GENERIC SINGLETON - Works with type parameters
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
public partial class Repository<T> where T : class, new()
{
    private readonly List<T> _items = [];
    // private readonly object _lock = new object();

    private void Initialize()
    {
        Console.WriteLine($"Repository<{typeof(T).Name}> initialized");
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            _items.Add(item);
        }
    }

    public IReadOnlyList<T> GetAll()
    {
        lock (_lock)
        {
            return _items.ToList().AsReadOnly();
        }
    }

    public T? FindById(Func<T, bool> predicate)
    {
        lock (_lock)
        {
            return _items.FirstOrDefault(predicate);
        }
    }
}