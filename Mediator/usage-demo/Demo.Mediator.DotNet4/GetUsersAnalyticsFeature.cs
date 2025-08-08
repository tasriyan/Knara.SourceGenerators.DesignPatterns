using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4
{
	[Query(Name = "GetUserCountQuery", ResponseType = typeof(int))]
	public class GetUserCountRequest
	{
		public DateTime CreatedAfter { get; set; }
	}

	[QueryHandler(Name="GetUserAnalyticsHandler", RequestType = typeof(GetUserCountQuery))]
	public class UserCountService(IUserRepository repository)
	{
		public async Task<int> GetAsync(GetUserCountRequest request, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Getting user count created after {request.CreatedAfter}");
			var count = await repository.GetCountAsync(request.CreatedAfter, cancellationToken);
			Console.WriteLine($"User count retrieved: {count}");
		
			return count;
		}
	}
}
