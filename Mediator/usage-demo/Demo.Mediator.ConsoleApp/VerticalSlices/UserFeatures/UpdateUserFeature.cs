using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures;

[Command(Name = "UpdateUserCommand", ResponseType = typeof(User))]
public record UpdateUserRequest(int UserId, string Email, string FirstName);

[CommandHandler(Name="UpdateUserCommandHandler", RequestType = typeof(UpdateUserCommand))]
public class UpdateUserService(IUserRepository repository, ILogger<UpdateUserService> logger)
{
	public async Task<User> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Updating user {UserId}", request.UserId);
		var user = await repository.GetByIdAsync(request.UserId, cancellationToken);
		if (user == null)
		{
			logger.LogWarning("User {UserId} not found for update", request.UserId);
			return User.UserNotFound(request.UserId);
		}

		user.Email = request.Email;
		user.FirstName = request.FirstName;
		user.UpdatedAt = DateTime.UtcNow;

		var updatedUser = await repository.UpdateAsync(user, cancellationToken);
		logger.LogInformation("User updated successfully: {UserId}", updatedUser.Id);
		return updatedUser;
	}
}