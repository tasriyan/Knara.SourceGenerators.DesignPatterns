using Knara.SourceGenerators.DesignPatterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4
{
	[Query(Name = "GetUserQuery", ResponseType = typeof(User))]
	public class GetUserRequest
	{
		public int UserId { get; set; }
	}

	[QueryHandler(Name="GetUserHandler", RequestType = typeof(GetUserQuery))]
	public class GetUserService(IUserRepository repository)
	{
		public async Task<User> GetAsync(GetUserRequest request, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Getting user {request.UserId}");

			var user = await repository.GetByIdAsync(request.UserId, cancellationToken);
			if (user == null)
			{
				Console.WriteLine($"User {request.UserId} not found");
				return User.UserNotFound(request.UserId);
			}

			Console.WriteLine($"Retrieved user {user.Email}");
			return user;
		}
	}
}
