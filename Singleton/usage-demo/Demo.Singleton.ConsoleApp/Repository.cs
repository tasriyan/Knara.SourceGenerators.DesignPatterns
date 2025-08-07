using System.Collections.Concurrent;
using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.ConsoleApp;

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

public class OrderEntity: IEntity
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public List<UserEntity> Users { get; set; } = [];
    public List<CustomerEntity> Customers { get; set; } = [];
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