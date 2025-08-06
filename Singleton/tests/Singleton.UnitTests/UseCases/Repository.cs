using CodeGenerator.Patterns.Singleton;

namespace Singleton.UnitTests.UseCases;

public interface IEntity
{
    int Id { get; set; }
}
public class UserEntity: IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CustomerEntity: IEntity
{
    public int Id { get; set; }
    public List<AddressEntity> Addresses { get; set; }
}

public class AddressEntity: IEntity
{
    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

// GENERIC SINGLETON - Works with type parameters
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
public partial class Repository<T> where T : IEntity
{
    private readonly List<T> _items = [];
    // private readonly object _lock = new object();

    private void Initialize()
    { Console.WriteLine($"Repository<{typeof(T).Name}> initialized");
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

//CONVERTED TO SINGLETON
partial class Repository<T> where T : IEntity
{
    private static volatile Repository<T>? _instance;
    private static readonly object _lock = new object();

    public static Repository<T> Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path

            lock (_lock)
            {
                if (_instance == null) // Double check inside lock
                {
                    _instance = CreateSingletonInstance();
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