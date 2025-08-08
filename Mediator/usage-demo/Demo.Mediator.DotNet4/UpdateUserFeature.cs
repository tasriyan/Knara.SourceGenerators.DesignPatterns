using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4
{
	[Command(Name = "UpdateUserCommand", ResponseType = typeof(User))]
	public class UpdateUserRequest
	{
		public int UserId { get; set; }
		public string Email { get; set; }
		public string FirstName { get; set; }
	}

	[CommandHandler(Name="UpdateUserCommandHandler", RequestType = typeof(UpdateUserCommand))]
	public class UpdateUserService(IUserRepository repository)
	{
		public async Task<User> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Updating user {request.UserId}");
			var user = await repository.GetByIdAsync(request.UserId, cancellationToken);
			if (user == null)
			{
				Console.WriteLine($"User {request.UserId} not found for update");
				return User.UserNotFound(request.UserId);
			}

			user.Email = request.Email;
			user.FirstName = request.FirstName;
			user.UpdatedAt = DateTime.UtcNow;

			var updatedUser = await repository.UpdateAsync(user, cancellationToken);
			Console.WriteLine($"User updated successfully: {updatedUser.Id}", updatedUser.Id);
			return updatedUser;
		}
	}
}