using Demo.Decorator.ConsoleApp.SampleServices;

namespace Demo.Services;

// Example usage in application
public class DecoratorUsageExample
{
    public static void ConfigureServices()
    {
        // Traditional approach - manual decorator chaining
        IUserService userService = new UserService();
        userService = new UserLoggingDecorator(userService, logger);
        userService = new UserCachingDecorator(userService, cache);
        userService = new UserValidationDecorator(userService);
        userService = new UserPerformanceDecorator(userService, logger);

        // Generated fluent approach - much cleaner!
        IUserService decoratedUserService = UserDecoratorFactory
            .Create(new UserService())
            .WithLogging(logger)
            .WithCaching(cache)
            .WithValidation()
            .WithPerformanceMonitoring(logger);

        // Or using dependency injection
        services.AddScoped<IUserService>(provider =>
        {
            var implementation = new UserService();
            var logger = provider.GetRequiredService<ILogger<UserService>>();
            var cache = provider.GetRequiredService<IMemoryCache>();

            return UserDecoratorFactory
                .Create(implementation)
                .WithLogging(logger)
                .WithCaching(cache)
                .WithValidation()
                .WithRetry()
                .WithPerformanceMonitoring(logger);
        });
    }

    public static async Task DemoUsage(IUserService userService)
    {
        // All decorators will automatically apply based on method attributes:
        // 1. Validation decorator validates parameters
        // 2. Logging decorator logs method entry/exit and errors  
        // 3. Performance decorator measures execution time
        // 4. Caching decorator caches results (for GetAll methods)
        // 5. Retry decorator retries on failure (for Create/Update methods)

        try
        {
            // This call will be:
            // - Validated (parameters checked)
            // - Logged (entry/exit/errors)
            // - Performance monitored (execution time measured)
            var user = await userService.GetUserByIdAsync(123);

            // This call will be:
            // - Cached (30 minute expiration)
            // - Logged (entry/exit/errors)
            var allUsers = await userService.GetAllUsersAsync();

            // This call will be:
            // - Validated (user object checked)
            // - Retried (up to 3 attempts with exponential backoff)
            // - Logged (entry/exit/errors)
            await userService.CreateUserAsync(new User
            {
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe"
            });
        }
        catch (Exception ex)
        {
            // All exceptions are logged by the logging decorator
            Console.WriteLine($"Operation failed: {ex.Message}");
        }
    }
}