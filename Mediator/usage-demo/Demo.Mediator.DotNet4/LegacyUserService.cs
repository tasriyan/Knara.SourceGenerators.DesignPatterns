using System.Runtime.CompilerServices;
using Knara.SourceGenerators.DesignPatterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4
{
	// LegacyUserService demonstrates how to use the legacy request handler attributes
// for handling user-related operations in a vertical slice architecture.
// It is designed to be compatible with the existing codebase while providing a clear
// structure for user management operations such as getting, creating, and updating users.
// This is what the legacy code looked like before the new patterns were introduced.
// It uses attributes like [RequestHandler] to define methods that handle specific requests.
	public class LegacyUserService(IUserRepository repository)
	{
		[RequestHandler(Name="LegacyUserGetHandler")]
		public async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Getting user {userId}");

			var user = await repository.GetByIdAsync(userId, cancellationToken);
			if (user == null)
			{
				Console.WriteLine($"User {userId} not found");
				return User.UserNotFound(userId);
			}

			Console.WriteLine($"Retrieved user {user.Email}");
			return user;
		}
	
		[RequestHandler(Name="LegacyUserCreateHandler")]	
		public async Task AddNewUserAsync(NewUserModel model, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Creating user with email {model.Email}");
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
				Console.WriteLine($"User {user.Email} created successfully");
			else
				Console.WriteLine($"Failed to create user {user.Email}");
		}
	
		[RequestHandler(Name="LegacyUserUpdateUserHandler")]
		public async Task<User> UpdateAsync(int userId, string email, string firstName, DateTime updateDate, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Updating user {userId}");
			var user = await repository.GetByIdAsync(userId, cancellationToken);
			if (user == null)
			{
				Console.WriteLine($"User {userId} not found for update");
				return User.UserNotFound(userId);
			}

			user.Email = email;
			user.FirstName = firstName;
			user.UpdatedAt = updateDate;

			var updatedUser = await repository.UpdateAsync(user, cancellationToken);
			Console.WriteLine($"User updated successfully: {updatedUser.Id}");
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
}
