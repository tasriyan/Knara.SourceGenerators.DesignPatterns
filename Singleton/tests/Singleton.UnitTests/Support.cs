namespace Singleton.UnitTests;

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