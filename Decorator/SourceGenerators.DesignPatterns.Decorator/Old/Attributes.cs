// namespace SourceGenerators.DesignPatterns.Decorator.Old;
//
// public class DecoratorAttribute
// {
// 	public string? BaseName { get; set; }
// 	public bool GenerateFactory { get; set; } = true;
// 	public bool GenerateAsyncSupport { get; set; } = true;
// 	public DecoratorAccessibility Accessibility { get; set; } = DecoratorAccessibility.Public;
// }
//
// public class DecoratorTypeAttribute
// {
// 	public string? Name { get; set; }
// 	public DecoratorType Type { get; set; }
// 	public string? LoggerProperty { get; set; }
// 	public string? CacheKeyFormat { get; set; }
// 	public int CacheExpirationMinutes { get; set; } = 60;
// 	public string? ValidationMethod { get; set; }
// 	public int RetryAttempts { get; set; } = 3;
// 	public string? MetricName { get; set; }
// }
//
// public enum DecoratorAccessibility
// {
// 	Public,
// 	Internal,
// 	Private
// }
//
// public enum DecoratorType
// {
// 	Logging,
// 	Caching,
// 	Validation,
// 	Retry,
// 	Performance,
// 	Authorization,
// 	CircuitBreaker
// }
