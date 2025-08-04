using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.Core;

public interface IUserRepository
{
	Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
	Task<bool> CreateAsync(User user, CancellationToken cancellationToken = default);
	Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
	Task<int> GetCountAsync(DateTime? createdAfter = null, CancellationToken cancellationToken = default);
	IAsyncEnumerable<User> GetUsersStreamAsync(string? emailFilter, DateTime? createdAfter, int pageSize, CancellationToken cancellationToken = default);
}

public class UserRepository(ILogger<UserRepository> logger) : IUserRepository
{
	// Simulating a database with an in-memory list for demonstration purposes
	private readonly List<User> _users = [];
	public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
	{
		logger.LogDebug("Fetching user by {UserId}:", id);
		
		// simulate async operation
		await Task.Delay(100, cancellationToken);
		
		return _users.FirstOrDefault(x => x.Id == id);
	}

	public async Task<bool> CreateAsync(User user, CancellationToken cancellationToken = default)
	{
		//if user exists, throw exception
		logger.LogDebug("Createing user with {Id}", user.Id);
		if (_users.Any(x => x.Id == user.Id))
		{
			throw new UserNotFoundException(userId: user.Id);
		}
		
		// Simulate async operation
		await Task.Delay(100, cancellationToken);
		
		_users.Add(user);
		logger.LogDebug("Created user {Id} with {Email}:", user.Id, user.Email);
		return true;
	}

	public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
	{
		logger.LogDebug("Updating user with {UserId}", user.Id);
		
		var existingUser = _users.FirstOrDefault(x => x.Id == user.Id);
		if (existingUser == null)
		{
			throw new UserNotFoundException(user.Id);
		}
		// Simulate async operation
		await Task.Delay(100, cancellationToken);
		
		existingUser.Email = user.Email;
		existingUser.FirstName = user.FirstName;
		existingUser.LastName = user.LastName;
		existingUser.UpdatedAt = DateTime.UtcNow;
		
		logger.LogDebug("User {UserId} updated successfully.", user.Id);
		return user;
	}

	public async Task<int> GetCountAsync(DateTime? createdAfter = null, CancellationToken cancellationToken = default)
	{
		logger.LogDebug("Getting user count with filter createdAfter {CreateAfterDt}", createdAfter);
		// Simulate async operation
		await Task.Delay(100, cancellationToken);
		
		return _users.Count(x => !createdAfter.HasValue || x.CreatedAt > createdAfter.Value);
	}

	public IAsyncEnumerable<User> GetUsersStreamAsync(string? emailFilter, DateTime? createdAfter, int pageSize,
		CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}