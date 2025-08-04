using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices;

[Query(Name = "GetUserQuery", ResponseType = typeof(User))]
public record GetUserRequest(int UserId);

[QueryHandler(Name="GetUserHandler", RequestType = typeof(GetUserQuery))]
public class GetUserService(IUserRepository repository, ILogger<GetUserService> logger)
{
	public async Task<User> GetAsync(GetUserRequest request, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Getting user {UserId}", request.UserId);

		var user = await repository.GetByIdAsync(request.UserId, cancellationToken);
		if (user == null)
		{
			logger.LogWarning("User {UserId} not found", request.UserId);
			return User.UserNotFound(request.UserId);
		}

		logger.LogInformation("Retrieved user {Email}", user.Email);
		return user;
	}
}
