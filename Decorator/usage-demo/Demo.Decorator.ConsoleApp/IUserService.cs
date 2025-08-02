using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SourceGenerators.DesignPatterns.Decorator;

namespace Demo.Decorator.ConsoleApp;

// Example 1: User Service with bunch of decorators

public class User
{
	public int Id { get; set; }
	public string Email { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public DateTime CreatedAt { get; set; }
}

[GenerateDecoratorFactory(BaseName = "User")]
public interface IUserService
{
	Task<User> GetUserByIdAsync(int userId);
	
	Task<List<User>> GetAllUsersAsync();
	
	Task CreateUserAsync(User user);
	
	Task UpdateUserAsync(User user);
	
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

[Decorator(Type = "Logging")]
public class UserLoggingDecorator: IUserService
{
	private readonly IUserService _inner;
	private readonly ILogger<IUserService> _logger;

	public UserLoggingDecorator(IUserService inner, ILogger<IUserService> logger)
	{
		_inner = inner;
		_logger = logger;
	}

	public async Task<User> GetUserByIdAsync(int userId)
	{
		_logger.LogInformation($"Getting user by ID: {userId}");
		var user = await _inner.GetUserByIdAsync(userId);
		_logger.LogInformation($"Retrieved user: {user.Email}");
		return user;
	}

	public async Task<List<User>> GetAllUsersAsync()
	{
		_logger.LogInformation("Getting all users");
		var users = await _inner.GetAllUsersAsync();
		_logger.LogInformation($"Retrieved {users.Count} users");
		return users;
	}

	public async Task CreateUserAsync(User user)
	{
		_logger.LogInformation($"Creating user: {user.Email}");
		await _inner.CreateUserAsync(user);
		_logger.LogInformation("User created successfully");
	}

	public async Task UpdateUserAsync(User user)
	{
		_logger.LogInformation($"Updating user: {user.Email}");
		await _inner.UpdateUserAsync(user);
		_logger.LogInformation("User updated successfully");
	}

	public async Task DeleteUserAsync(int userId)
	{
		_logger.LogInformation($"Deleting user with ID: {userId}");
		await _inner.DeleteUserAsync(userId);
	}
}

[Decorator(Type = "Caching")]
public class UserCachingDecorator : IUserService
{
	private readonly IUserService _inner;
	private readonly IMemoryCache _cache;

	public UserCachingDecorator(IUserService inner, IMemoryCache cache)
	{
		_inner = inner;
		_cache = cache;
	}

	public async Task<User> GetUserByIdAsync(int userId)
	{
		if (_cache.TryGetValue(userId, out User cachedUser))
		{
			return cachedUser;
		}

		var user = await _inner.GetUserByIdAsync(userId);
		_cache.Set(userId, user, TimeSpan.FromMinutes(30));
		return user;
	}

	public async Task<List<User>> GetAllUsersAsync()
	{
		if (_cache.TryGetValue("AllUsers", out List<User> cachedUsers))
		{
			return cachedUsers;
		}

		var users = await _inner.GetAllUsersAsync();
		_cache.Set("AllUsers", users, TimeSpan.FromMinutes(30));
		return users;
	}

	public Task CreateUserAsync(User user) => _inner.CreateUserAsync(user);
	public Task UpdateUserAsync(User user) => _inner.UpdateUserAsync(user);
	public Task DeleteUserAsync(int userId) => _inner.DeleteUserAsync(userId);
}

[Decorator(Type = "Validation")]
public class UserValidationDecorator : IUserService
{
	private readonly IUserService _inner;

	public UserValidationDecorator(IUserService inner)
	{
		_inner = inner;
	}

	public async Task<User> GetUserByIdAsync(int userId)
	{
		if (userId <= 0) throw new ArgumentException("Invalid user ID");
		return await _inner.GetUserByIdAsync(userId);
	}

	public async Task<List<User>> GetAllUsersAsync()
	{
		return await _inner.GetAllUsersAsync();
	}

	public async Task CreateUserAsync(User user)
	{
		if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException("Email is required");
		await _inner.CreateUserAsync(user);
	}

	public async Task UpdateUserAsync(User user)
	{
		if (user.Id <= 0) throw new ArgumentException("Invalid user ID");
		await _inner.UpdateUserAsync(user);
	}

	public async Task DeleteUserAsync(int userId)
	{
		if (userId <= 0) throw new ArgumentException("Invalid user ID");
		await _inner.DeleteUserAsync(userId);
	}
}

[Decorator(Type = "PerformanceMonitoring")]
public class UserPerformanceMonitoringDecorator : IUserService
{
	private readonly IUserService _inner;
	private readonly ILogger<UserPerformanceMonitoringDecorator> _logger;

	public UserPerformanceMonitoringDecorator(IUserService inner, ILogger<UserPerformanceMonitoringDecorator> logger)
	{
		_inner = inner;
		_logger = logger;
	}

	public async Task<User> GetUserByIdAsync(int userId)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var user = await _inner.GetUserByIdAsync(userId);
		stopwatch.Stop();
		_logger.LogInformation($"GetUserByIdAsync executed in {stopwatch.ElapsedMilliseconds} ms");
		return user;
	}

	public async Task<List<User>> GetAllUsersAsync()
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var users = await _inner.GetAllUsersAsync();
		stopwatch.Stop();
		_logger.LogInformation($"GetAllUsersAsync executed in {stopwatch.ElapsedMilliseconds} ms");
		return users;
	}

	public async Task CreateUserAsync(User user)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		await _inner.CreateUserAsync(user);
		stopwatch.Stop();
		_logger.LogInformation($"CreateUserAsync executed in {stopwatch.ElapsedMilliseconds} ms");
	}

	public async Task UpdateUserAsync(User user)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		await _inner.UpdateUserAsync(user);
		stopwatch.Stop();
		_logger.LogInformation($"UpdateUserAsync executed in {stopwatch.ElapsedMilliseconds} ms");
	}

	public async Task DeleteUserAsync(int userId)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		await _inner.DeleteUserAsync(userId);
		stopwatch.Stop();
		_logger.LogInformation($"DeleteUserAsync executed in {stopwatch.ElapsedMilliseconds} ms");
	}
}

public static class UserServiceDemo
{
	// This method shows both manual decorator chaining and the cleaner fluent API approach.
	public static async Task FluidApiUsageDemo(ILoggerFactory loggerFactory)
	{
		// Setup dependencies
		var logger = loggerFactory.CreateLogger<IUserService>();
		var cache = new MemoryCache(new MemoryCacheOptions());

		// Create decorated service using the generated factory
		IUserService userService = UserDecoratorFactory
			.Create(new UserService())
			.WithValidation()
			.WithCaching(cache)
			.WithLogging(logger);

		// Use the service
		var user = await userService.GetUserByIdAsync(1);

		var users = await userService.GetAllUsersAsync();

		await userService.CreateUserAsync(new User 
		{ 
			Email = "newuser@example.com",
			FirstName = "New",
			LastName = "User"
		});
	}
	
	// This method demonstrates how to use the generated decorator factory in a real application.
	public static IServiceCollection DependencyInjectionUsage(this IServiceCollection services)
	{
		services.AddScoped<IUserService>(provider =>
		{
			var implementation = new UserService();
			var logger = provider.GetRequiredService<ILogger<UserService>>();
			var performanceLogger = provider.GetRequiredService<ILogger<UserPerformanceMonitoringDecorator>>();
			var cache = provider.GetRequiredService<IMemoryCache>();

			return UserDecoratorFactory
				.Create(implementation)
				.WithValidation()
				.WithCaching(cache)
				.WithPerformanceMonitoring(performanceLogger)
				.WithLogging(logger);
		});
		
		return services;
	}
}