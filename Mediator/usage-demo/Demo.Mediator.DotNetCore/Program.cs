using Microsoft.Extensions.DependencyInjection;
using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.DotNetCore;
using Demo.Mediator.DotNetCore.VerticalSlices.UserFeatures;
using Demo.Mediator.DotNetCore.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

var services = ServiceCollectionExtensions.RegisterServices();

using var scope = services.BuildServiceProvider().CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

var demo = new MediatorWithCQRSDemo(mediator);
await demo.DemonstrateUsage();

var legacyDemo = new MediatorWithLegacyCodeConversionDemo(mediator);
await legacyDemo.DemonstrateUsage();

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

public class MediatorWithCQRSDemo(IMediator mediator)
{
	public async Task DemonstrateUsage()
	{
		Console.WriteLine("\n=================================");
		Console.WriteLine("Source Generated Mediator Demo with CQS and Domain Events");
		Console.WriteLine("=================================");

		var random = new Random();
		var userId = random.Next(1000, 9999);
		
		// 1. Command - Fire and forget
		Console.WriteLine("\n[1] Command Example:");
		var success =await mediator.Send(new CreateUserCommand
			{
				UserId = userId,
				Email = "arabella@example.com",
				FirstName = "Arabella",
				LastName = "Smith"
		});
		// 2. Notifications - publish/subscribe
		if (success)
		{
			Console.WriteLine($"User created successfully with ID: {userId}");
			
			// Publish domain event 
			await mediator.Publish(new UserCreatedEvent
			{ 
				UserId = userId,
				Timestamp = DateTime.UtcNow 
			});
		}
		else
		{
			Console.WriteLine($"Failed to create user with ID: {userId}");
		}
		
		// 3. Query - Request/Response
		Console.WriteLine("\n[2] Query Example:");
		var user = await mediator.Send(new GetUserQuery {
			UserId = userId
			});

		// 4. Command with result
		Console.WriteLine("\n[3] Command with Result:");
		var updatedUser = await mediator.Send(new UpdateUserCommand
			{
			UserId = userId,
			Email = "arabella.smith@example.com",
			FirstName = "Bella"
		});
		if (updatedUser != null)
		{
			Console.WriteLine($"User updated successfully: {updatedUser.FirstName} {updatedUser.LastName} ({updatedUser.Email})");
		}
		else
		{
			Console.WriteLine($"Failed to update user with ID: {userId}");
		}
		
		// 5. Analytics query
		Console.WriteLine("\n[4] Analytics Query:");
		var createdAfter = DateTime.Now.AddDays(-3);
		var userCount = await mediator.Send(new GetUserCountQuery 
		{ 
			CreatedAfter = createdAfter
		});
		Console.WriteLine($"Total users created after {createdAfter}: {userCount}");

		// 6. Cancellation support
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
		
		// 7. Streaming query
		Console.WriteLine("\n[6] Streaming Query:");
		await foreach (var streamUser in mediator.CreateStream(new UsersStreamQuery
	       {
	           EmailFilter = "@example.com",
	           BufferSize = 2
	       }))
		{
			Console.WriteLine($"Streaming user: {streamUser.Email}");
		}
	}
}

public class MediatorWithLegacyCodeConversionDemo(IMediator mediator)
{
	public async Task DemonstrateUsage()
	{
		Console.WriteLine("\n=================================");
		Console.WriteLine("Source Generated Mediator Demo with Legacy Code Conversion");
		Console.WriteLine("=================================");

		var random = new Random();
		var userId = random.Next(1000, 9999);
		
		// 1. Create a new user
		await mediator.Send(
			new LegacyUserCreateRequest
			{
				Model = new NewUserModel
				{
					UserId = userId,
					Email = "arabella@example.com",
					FirstName = "Arabella",
					LastName = "Smith"
				}
			});
		
		// 3. Fetch user by id
		var user = await mediator.Send(new LegacyUserGetRequest {
			UserId = userId
		});

		// 4. UpdateUser
		var updatedUser = await mediator.Send(new LegacyUserUpdateUserRequest
		{
			UserId = userId,
			Email = "arabella.smith@example.com",
			FirstName = "Bella"
		});
		if (updatedUser != null)
		{
			Console.WriteLine($"User updated successfully: {updatedUser.FirstName} {updatedUser.LastName} ({updatedUser.Email})");
		}
		else
		{
			Console.WriteLine($"Failed to update user with ID: {userId}");
		}
	}
}

