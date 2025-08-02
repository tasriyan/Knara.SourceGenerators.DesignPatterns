using Demo.Generator;

namespace Demo.Decorator.ConsoleApp.SampleServices
{
	// Example 3: Data Repository with caching and performance monitoring
	[GenerateDecorators(BaseName = "Repository", Accessibility = DecoratorAccessibility.Internal)]
	public interface IDataRepository
	{
		[Decorator(Type = DecoratorType.Caching, CacheExpirationMinutes = 15)]
		[Decorator(Type = DecoratorType.Performance)]
		[Decorator(Type = DecoratorType.Logging)]
		Task<T> GetByIdAsync<T>(int id) where T : class;

		[Decorator(Type = DecoratorType.Caching, CacheExpirationMinutes = 5)]
		[Decorator(Type = DecoratorType.Performance)]
		List<T> GetAll<T>() where T : class;

		[Decorator(Type = DecoratorType.Validation)]
		[Decorator(Type = DecoratorType.Retry, RetryAttempts = 2)]
		[Decorator(Type = DecoratorType.Performance)]
		Task SaveAsync<T>(T entity) where T : class;

		[Decorator(Type = DecoratorType.Logging)]
		[Decorator(Type = DecoratorType.Performance)]
		Task DeleteAsync<T>(int id) where T : class;
	}
}
