namespace Demo.Decorator.ConsoleApp.SampleServices;

public class User
{
	public int Id { get; set; }
	public string Email { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public DateTime CreatedAt { get; set; }
}


// Example 1: User Service with comprehensive decorators
[GenerateDecorators(BaseName = "User", GenerateFactory = true)]
public interface IUserService
{
	[Decorator(Type = DecoratorType.Logging)]
	[Decorator(Type = DecoratorType.Validation)]
	[Decorator(Type = DecoratorType.Performance)]
	Task<User> GetUserByIdAsync(int userId);

	[Decorator(Type = DecoratorType.Caching, CacheExpirationMinutes = 30)]
	[Decorator(Type = DecoratorType.Logging)]
	Task<List<User>> GetAllUsersAsync();

	[Decorator(Type = DecoratorType.Validation)]
	[Decorator(Type = DecoratorType.Retry, RetryAttempts = 3)]
	[Decorator(Type = DecoratorType.Logging)]
	Task CreateUserAsync(User user);

	[Decorator(Type = DecoratorType.Validation)]
	[Decorator(Type = DecoratorType.Performance)]
	Task UpdateUserAsync(User user);

	[Decorator(Type = DecoratorType.Logging)]
	Task DeleteUserAsync(int userId);
}

public class UserService : IUserService
{
	public async Task<User> GetUserByIdAsync(int userId)
	{
		// Simulate database call
		await Task.Delay(100);
		return new User { Id = userId, Email = "user@example.com" };
	}

	public async Task<List<User>> GetAllUsersAsync()
	{
		// Simulate database call
		await Task.Delay(200);
		return new List<User>
		{
			new User { Id = 1, Email = "user1@example.com" },
			new User { Id = 2, Email = "user2@example.com" }
		};
	}

	public async Task CreateUserAsync(User user)
	{
		// Simulate database call
		await Task.Delay(150);
	}

	public async Task UpdateUserAsync(User user)
	{
		// Simulate database call
		await Task.Delay(120);
	}

	public async Task DeleteUserAsync(int userId)
	{
		// Simulate database call
		await Task.Delay(80);
	}
}
