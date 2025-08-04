using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices;

[Query(Name = "GetUserCountQuery", ResponseType = typeof(int))]
public record GetUserCountRequest(DateTime CreatedAfter);

[QueryHandler(Name="GetUserAnalyticsHandler", RequestType = typeof(GetUserCountQuery))]
public class UserCountService(IUserRepository repository, ILogger<UserCountService> logger)
{
	public async Task<int> GetAsync(GetUserCountRequest request, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Getting user count created after {CreatedAfter}", request.CreatedAfter);
		var count = await repository.GetCountAsync(request.CreatedAfter, cancellationToken);
		logger.LogInformation("User count retrieved: {Count}", count);
		
		return count;
	}
}
