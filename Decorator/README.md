# Decorator Pattern Source Generator

A C# source generator that automatically creates decorator classes for cross-cutting concerns like logging, caching, validation, retry logic, and performance monitoring.

## Why Use This?

**Manual Decorator Problems:**
- Tons of boilerplate forwarding methods
- Easy to forget implementing new interface methods  
- Inconsistent decorator implementations
- Runtime reflection overhead

**Source Generation Benefits:**
- ✅ Zero boilerplate - generates all forwarding automatically
- ✅ Compile-time safety - catches missing methods immediately
- ✅ Perfect IntelliSense support
- ✅ Zero runtime overhead
- ✅ Consistent, tested implementations

## Usage

Mark your interface with `[GenerateDecorators]` and methods with `[Decorator]`:

```csharp
[GenerateDecorators(BaseName = "User")]
public interface IUserService
{
    [Decorator(Type = DecoratorType.Logging)]
    [Decorator(Type = DecoratorType.Caching, CacheExpirationMinutes = 30)]
    [Decorator(Type = DecoratorType.Performance)]
    Task<User> GetUserAsync(int id);

    [Decorator(Type = DecoratorType.Validation)]
    [Decorator(Type = DecoratorType.Retry, RetryAttempts = 3)]
    Task CreateUserAsync(User user);
}
```

Use the generated fluent factory:

```csharp
IUserService service = UserDecoratorFactory
    .Create(new UserService())
    .WithLogging(logger)
    .WithCaching(cache)
    .WithValidation()
    .WithPerformanceMonitoring(logger);

// Now all calls automatically get:
// - Logging (entry/exit/errors)
// - Caching (with 30min expiration)  
// - Performance monitoring (execution time)
// - Validation (null checks)
// - Retry logic (3 attempts with exponential backoff)
```

## Generated Decorators

| Decorator Type | Purpose | Features |
|---|---|---|
| **Logging** | Method tracing | Entry/exit logging, error logging, parameter logging |
| **Caching** | Result caching | Configurable expiration, cache key generation |
| **Validation** | Input validation | Null checks, custom validation methods |
| **Retry** | Fault tolerance | Exponential backoff, configurable attempts |
| **Performance** | Monitoring | Execution time measurement, performance logging |

## Dependency Injection Integration

```csharp
services.AddScoped<IUserService>(provider =>
{
    var implementation = new UserService();
    var logger = provider.GetService<ILogger<UserService>>();
    var cache = provider.GetService<IMemoryCache>();

    return UserDecoratorFactory
        .Create(implementation)
        .WithLogging(logger)
        .WithCaching(cache)
        .WithValidation()
        .WithRetry()
        .WithPerformanceMonitoring(logger);
});
```

## Real-World Benefits

**Before (Manual):**
```csharp
// 50+ lines of boilerplate per decorator per method
public class UserLoggingDecorator : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger _logger;
    
    public UserLoggingDecorator(IUserService inner, ILogger logger) { ... }
    
    public async Task<User> GetUserAsync(int id)
    {
        _logger.LogInformation("Getting user {Id}", id);
        try
        {
            var result = await _inner.GetUserAsync(id);
            _logger.LogInformation("Successfully got user {Id}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {Id}", id);
            throw;
        }
    }
    
    // Repeat for every single method... 😫
}
```

**After (Generated):**
```csharp
[Decorator(Type = DecoratorType.Logging)]
Task<User> GetUserAsync(int id);
// Done! 🎉
```

The generator creates production-ready decorators with proper async support, exception handling, and performance optimizations.