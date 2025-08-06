using System.Runtime.CompilerServices;
using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures;
using Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp;

// LegacyUserService demonstrates how to use the legacy request handler attributes
// for handling user-related operations in a vertical slice architecture.
// It is designed to be compatible with the existing codebase while providing a clear
// structure for user management operations such as getting, creating, and updating users.
// This is what the legacy code looked like before the new patterns were introduced.
// It uses attributes like [RequestHandler] to define methods that handle specific requests.
public class LegacyUserService(IUserRepository repository, ILogger<LegacyUserService> logger)
{
	[RequestHandler(Name="LegacyUserGetHandler")]
	public async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Getting user {UserId}", userId);

		var user = await repository.GetByIdAsync(userId, cancellationToken);
		if (user == null)
		{
			logger.LogWarning("User {UserId} not found", userId);
			return User.UserNotFound(userId);
		}

		logger.LogInformation("Retrieved user {Email}", user.Email);
		return user;
	}
	
	[RequestHandler(Name="LegacyUserCreateHandler")]	
	public async Task AddNewUserAsync(NewUserModel model, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Creating user with email {Email}", model.Email);
		var user = new User
		{
			Id = model.UserId,
			Email = model.Email,
			FirstName = model.FirstName,
			LastName = model.LastName,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		var success = await repository.CreateAsync(user, cancellationToken);
		if (success)
			logger.LogInformation("User {Email} created successfully", user.Email);
        else
			logger.LogError("Failed to create user {Email}", user.Email);
	}
	
	[RequestHandler(Name="LegacyUserUpdateUserHandler")]
	public async Task<User> UpdateAsync(int userId, string email, string firstName, DateTime updateDate, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Updating user {UserId}", userId);
		var user = await repository.GetByIdAsync(userId, cancellationToken);
		if (user == null)
		{
			logger.LogWarning("User {UserId} not found for update", userId);
			return User.UserNotFound(userId);
		}

		user.Email = email;
		user.FirstName = firstName;
		user.UpdatedAt = updateDate;

		var updatedUser = await repository.UpdateAsync(user, cancellationToken);
		logger.LogInformation("User updated successfully: {UserId}", updatedUser.Id);
		return updatedUser;
	}
	
	// No mediator pattern for this method, this method will run as is, not to be invoked by mediator
	public async IAsyncEnumerable<User> GetAsync(BulkUsersRequestWithBuffer request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var bufferSize = GetEffectiveBufferSize(request);
		
		await foreach (var user in repository.GetFilteredUsersAsync(request.EmailFilter, 
			               bufferSize,
			               cancellationToken))
		{
			yield return user;
		}
	}
	
	private int GetEffectiveBufferSize(BulkUsersRequestWithBuffer bufferedRequest)
	{
		if (bufferedRequest.BufferSize.HasValue)
		{
			return bufferedRequest.BufferSize.Value;
		}
		
		return 50; 
	}
}

public class NewUserModel
{
	public int UserId { get; set; }
	public string Email { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
}
