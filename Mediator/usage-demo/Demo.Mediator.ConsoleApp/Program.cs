using Demo.Mediator.ConsoleApp.VerticalSlices;
using Microsoft.Extensions.DependencyInjection;
using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.Core;
using Microsoft.Extensions.Logging;

var services = ServiceCollectionExtensions.RegisterServices();

using var scope = services.BuildServiceProvider().CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

var demo = new MediatorUsageDemo(mediator);
await demo.DemonstrateUsage();

public static class ServiceCollectionExtensions
{
	public static IServiceCollection RegisterServices()
	{
		var services =new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder
				.AddConsole()
				.SetMinimumLevel(LogLevel.Debug);
    
			// Ensure console provider is configured properly
			builder.AddSimpleConsole(options =>
			{
				options.IncludeScopes = false;
				options.SingleLine = true;
				options.TimestampFormat = "HH:mm:ss ";
			});
		});
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddDeclarativeMediator();
		
		return services;
	}
}

public class MediatorUsageDemo(IMediator mediator)
{
	public async Task DemonstrateUsage()
	{
		Console.WriteLine("Source Generated Mediator Demo");
		Console.WriteLine("=================================");

		var random = new Random();
		var userId = random.Next(1000, 9999);
		
		// 1. Command - Fire and forget
		Console.WriteLine("\n[1] Command Example:");
		await mediator.Send(new CreateUserCommand
			{
			UserId = userId,
			Email = "john@example.com",
			FirstName = "John",
			LastName = "Doe"
		});
		
		// 2. Query - Request/Response
		Console.WriteLine("\n[2] Query Example:");
		var user = await mediator.Send(new GetUserQuery {
			UserId = userId
			});

		// 3. Command with result
		Console.WriteLine("\n[3] Command with Result:");
		var updatedUser = await mediator.Send(new UpdateUserCommand
			{
			UserId = userId,
			Email = "john.doe@example.com",
			FirstName = "John"
		});
		// 4. Performance-critical query
		Console.WriteLine("\n[4] Performance Query:");
		var userCount = await mediator.Send(new GetUserCountQuery 
		{ 
			CreatedAfter =DateTime.Now.AddSeconds(-10)
		});
		Console.WriteLine($"Total users: {userCount}");

		// 5. Cancellation support
		Console.WriteLine("\n[5] Cancellation Example:");
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		try
		{
			var userWithTimeout = await mediator.Send(
				new GetUserQuery {
						UserId = 789 
				}, cts.Token);
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("Operation was cancelled");
		}
		// 6. Notification - One-to-many
		// Console.WriteLine("\n4️⃣ Notification Example:");
		// await _mediator.Publish(new UserCreatedEvent { UserId = 456 });
		// Console.WriteLine("Notification published to all handlers");

		// 7. Streaming query
		// Console.WriteLine("\n5️⃣ Streaming Query:");
		// await foreach (var streamUser in _mediator.CreateStream(new GetUsersStreamQuery
		// {
		// 	EmailFilter = "@example.com",
		// 	PageSize = 50
		// }))
		// {
		// 	Console.WriteLine($"Streaming user: {streamUser.Email}");
		// }
	}
}

