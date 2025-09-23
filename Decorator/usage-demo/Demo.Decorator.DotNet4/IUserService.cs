using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Knara.SourceGenerators.DesignPatterns.Decorator;

namespace Demo.Decorator.DotNet4
{
	// Example 1: User Service with a bunch of decorators

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

		public UserLoggingDecorator(IUserService inner)
		{
			_inner = inner;
		}

		public async Task<User> GetUserByIdAsync(int userId)
		{
			Console.WriteLine($"Getting user by ID: {userId}");
			var user = await _inner.GetUserByIdAsync(userId);
			Console.WriteLine($"Retrieved user: {user.Email}");
			return user;
		}

		public async Task<List<User>> GetAllUsersAsync()
		{
			Console.WriteLine("Getting all users");
			var users = await _inner.GetAllUsersAsync();
			Console.WriteLine($"Retrieved {users.Count} users");
			return users;
		}

		public async Task CreateUserAsync(User user)
		{
			Console.WriteLine($"Creating user: {user.Email}");
			await _inner.CreateUserAsync(user);
			Console.WriteLine("User created successfully");
		}

		public async Task UpdateUserAsync(User user)
		{
			Console.WriteLine($"Updating user: {user.Email}");
			await _inner.UpdateUserAsync(user);
			Console.WriteLine("User updated successfully");
		}

		public async Task DeleteUserAsync(int userId)
		{
			Console.WriteLine($"Deleting user with ID: {userId}");
			await _inner.DeleteUserAsync(userId);
		}
	}
	
	public interface IMemoryCache
	{
		bool TryGetValue<T>(object key, out T value);
		void Set<T>(object key, T value, TimeSpan expiration);
	}
	
	public class MemoryCache : IMemoryCache
	{
		private readonly Dictionary<object, (object Value, DateTime Expiration)> _cache = new();

		public bool TryGetValue<T>(object key, out T value)
		{
			if (_cache.TryGetValue(key, out var entry) && entry.Expiration > DateTime.UtcNow)
			{
				value = (T)entry.Value;
				return true;
			}

			value = default;
			return false;
		}

		public void Set<T>(object key, T value, TimeSpan expiration)
		{
			_cache[key] = (value, DateTime.UtcNow.Add(expiration));
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

		public UserPerformanceMonitoringDecorator(IUserService inner)
		{
			_inner = inner;
		}

		public async Task<User> GetUserByIdAsync(int userId)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var user = await _inner.GetUserByIdAsync(userId);
			stopwatch.Stop();
			Console.WriteLine($"GetUserByIdAsync executed in {stopwatch.ElapsedMilliseconds} ms");
			return user;
		}

		public async Task<List<User>> GetAllUsersAsync()
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var users = await _inner.GetAllUsersAsync();
			stopwatch.Stop();
			Console.WriteLine($"GetAllUsersAsync executed in {stopwatch.ElapsedMilliseconds} ms");
			return users;
		}

		public async Task CreateUserAsync(User user)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			await _inner.CreateUserAsync(user);
			stopwatch.Stop();
			Console.WriteLine($"CreateUserAsync executed in {stopwatch.ElapsedMilliseconds} ms");
		}

		public async Task UpdateUserAsync(User user)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			await _inner.UpdateUserAsync(user);
			stopwatch.Stop();
			Console.WriteLine($"UpdateUserAsync executed in {stopwatch.ElapsedMilliseconds} ms");
		}

		public async Task DeleteUserAsync(int userId)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			await _inner.DeleteUserAsync(userId);
			stopwatch.Stop();
			Console.WriteLine($"DeleteUserAsync executed in {stopwatch.ElapsedMilliseconds} ms");
		}
	}

	public static class UserServiceDemo
	{
		// This method shows both manual decorator chaining and the cleaner fluent API approach.
		public static async Task FluidApiUsageDemo()
		{
			// Create decorated service using the generated factory
			IUserService userService = UserDecoratorFactory
				.Create(new UserService())
				.WithValidation()
				.WithCaching(new MemoryCache())
				.WithLogging();

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
	}
}